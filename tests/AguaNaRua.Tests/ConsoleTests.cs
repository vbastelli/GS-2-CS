using System.Net;
using System.Text;
using System.Text.Json;
using AguaNaRua.Api.Infrastructure;
using AguaNaRua.ConsoleApp;
using Microsoft.Extensions.DependencyInjection;

namespace AguaNaRua.Tests;

public sealed class ConsoleTests
{
    [Fact]
    public void Configuracao_padrao_e_substituicao_de_endereco()
    {
        Assert.Equal("http://localhost:5080/", ConfiguracaoConsole.Ler([]).ApiUrl.AbsoluteUri);
        Assert.Equal("https://exemplo.test/base/", ConfiguracaoConsole.Ler(["--api-url", "https://exemplo.test/base"]).ApiUrl.AbsoluteUri);
        Assert.True(ConfiguracaoConsole.Ler(["--help"]).Ajuda);
    }

    [Theory]
    [InlineData("")]
    [InlineData("localhost:5080")]
    [InlineData("ftp://localhost")]
    [InlineData("http://localhost:0")]
    [InlineData("http://usuario:senha@localhost:5080")]
    [InlineData("http://localhost:5080?token=abc")]
    [InlineData("http://localhost:5080/#fragmento")]
    public void Rejeita_url_invalida(string url)
        => Assert.Throws<ArgumentException>(() => ConfiguracaoConsole.Ler(["--api-url", url]));

    [Fact]
    public void Rejeita_argumentos_ausentes_ou_desconhecidos()
    {
        Assert.Throws<ArgumentException>(() => ConfiguracaoConsole.Ler(["--api-url"]));
        Assert.Throws<ArgumentException>(() => ConfiguracaoConsole.Ler(["--outra", "1"]));
    }

    [Theory]
    [InlineData("2026-11-19T10:00:00-03:00", true)]
    [InlineData("2026-11-19T13:00:00.1234567Z", true)]
    [InlineData("2026-11-19T13:00:00", false)]
    [InlineData("19/11/2026 10:00", false)]
    [InlineData("2026-02-30T13:00:00Z", false)]
    public void Horario_exige_timezone_explicito_e_preserva_instante(string texto, bool valido)
    {
        Assert.Equal(valido, HorarioIso8601.TentarLer(texto, out var instante));
        if (valido) Assert.StartsWith("2026-11-19T13:00:00", HorarioIso8601.Exibir(instante));
    }

    [Fact]
    public async Task Post_envia_somente_dois_campos_com_enum_textual_e_instante_UTC()
    {
        string? enviado = null;
        using var handler = new Handler(async (request, token) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/base/api/pontos/1/observacoes", request.RequestUri!.AbsolutePath);
            enviado = await request.Content!.ReadAsStringAsync(token);
            return JsonResponse(HttpStatusCode.Created, ObservacaoJson);
        });
        using var http = NovoHttp(handler, "http://localhost/base/");
        var resposta = await new AguaNaRuaClient(http).RegistrarAsync(1,
            new RegistrarObservacaoDto(CondicaoRelatada.AGUA_ACUMULADA,
                new DateTimeOffset(2026, 11, 18, 11, 0, 0, TimeSpan.FromHours(-3))), CancellationToken.None);
        using var json = JsonDocument.Parse(enviado!);
        Assert.Equal(2, json.RootElement.EnumerateObject().Count());
        Assert.Equal("AGUA_ACUMULADA", json.RootElement.GetProperty("condicao").GetString());
        Assert.Equal("2026-11-18T14:00:00.0000000Z", json.RootElement.GetProperty("observadoEm").GetString());
        Assert.Equal(OrigemObservacao.RELATO_CIDADAO, resposta.Origem);
    }

    [Theory]
    [InlineData("não é JSON")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[null]")]
    [InlineData("[{\"id\":1}]")]
    public async Task JSON_incompativel_retorna_mensagem_e_menu_continua(string corpo)
    {
        using var handler = new Handler((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK, corpo)));
        using var http = NovoHttp(handler);
        var texto = await ExecutarAsync(http, "1\n0\n");
        Assert.Contains("JSON inválido ou incompatível", texto);
        Assert.Contains("Até logo!", texto);
        Assert.Equal(1, handler.Chamadas);
    }

    [Theory]
    [InlineData(400, "{\"title\":\"Dados inválidos.\",\"errors\":{\"observadoEm\":[\"Não pode estar no futuro.\"]}}", "observadoEm: Não pode estar no futuro.")]
    [InlineData(404, "{\"title\":\"Recurso não encontrado.\"}", "Recurso não encontrado.")]
    [InlineData(500, "<html>erro</html>", "Resposta inesperada da API.")]
    public async Task Erros_HTTP_sao_apresentados_sem_tentativas_automaticas(int status, string corpo, string esperado)
    {
        using var handler = new Handler((_, _) => Task.FromResult(JsonResponse((HttpStatusCode)status, corpo)));
        using var http = NovoHttp(handler);
        var texto = await ExecutarAsync(http, "2\n1\n0\n");
        Assert.Contains($"HTTP {status}", texto);
        Assert.Contains(esperado, texto);
        Assert.Equal(1, handler.Chamadas);
    }

    [Fact]
    public async Task Lista_vazia_e_media_type_inesperado_sao_tratados()
    {
        using var handler = new Handler((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.OK, "[]")));
        using var http = NovoHttp(handler);
        Assert.Contains("Nenhum ponto monitorado cadastrado", await ExecutarAsync(http, "1\n0\n"));
        using var outroHandler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("<html>não é a API</html>", Encoding.UTF8, "text/html")
        }));
        using var outroHttp = NovoHttp(outroHandler);
        Assert.Contains("não retornou conteúdo JSON", await ExecutarAsync(outroHttp, "1\n0\n"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("2\n")]
    [InlineData("4\n1\n2\n2\n")]
    public async Task Fim_da_entrada_inclusive_dentro_da_operacao_encerra_sem_loop(string entrada)
    {
        using var handler = new Handler((_, _) => throw new InvalidOperationException("Não deveria chamar HTTP."));
        using var http = NovoHttp(handler);
        Assert.Contains("Entrada encerrada", await ExecutarAsync(http, entrada));
        Assert.Equal(0, handler.Chamadas);
    }

    [Fact]
    public async Task Escolhas_vazias_e_ID_invalido_nao_encerram_menu()
    {
        using var handler = new Handler((_, _) => Task.FromResult(JsonResponse(HttpStatusCode.NotFound, "{}")));
        using var http = NovoHttp(handler);
        var texto = await ExecutarAsync(http, "\n9\n2\n\nabc\n-1\n999\n0\n");
        Assert.Contains("Opção inválida", texto);
        Assert.Contains("ID inválido", texto);
        Assert.Contains("HTTP 404", texto);
        Assert.Contains("Até logo!", texto);
        Assert.Equal(1, handler.Chamadas);
    }

    [Fact]
    public async Task API_desligada_mostra_URL_e_comando_de_inicializacao()
    {
        using var handler = new Handler((_, _) => throw new HttpRequestException("conexão recusada"));
        using var http = NovoHttp(handler, "http://localhost:5999/");
        var texto = await ExecutarAsync(http, "1\n0\n");
        Assert.Contains("Não foi possível acessar a API em http://localhost:5999/", texto);
        Assert.Contains("dotnet run --project src/AguaNaRua.Api --launch-profile http", texto);
        Assert.Contains("Até logo!", texto);
        Assert.Equal(1, handler.Chamadas);
    }

    [Fact]
    public async Task Timeout_e_distinto_de_cancelamento_do_usuario()
    {
        using var handler = new Handler(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return JsonResponse(HttpStatusCode.OK, "[]");
        });
        using var http = NovoHttp(handler);
        http.Timeout = TimeSpan.FromMilliseconds(100);
        var texto = await ExecutarAsync(http, "1\n0\n");
        Assert.Contains("Tempo limite", texto);
        Assert.Contains("Até logo!", texto);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancelado = await ExecutarAsync(http, "1\n", cts.Token);
        Assert.Contains("Operação cancelada", cancelado);
    }

    [Fact]
    public async Task Historico_navega_nas_duas_direcoes_e_impede_pagina_inexistente()
    {
        var paginas = new List<int>();
        using var handler = new Handler((request, _) =>
        {
            var pagina = request.RequestUri!.Query.Contains("page=2&", StringComparison.Ordinal) ? 2 : 1;
            paginas.Add(pagina);
            return Task.FromResult(JsonResponse(HttpStatusCode.OK,
                $$"""{"itens":[{{ObservacaoJson}}],"page":{{pagina}},"pageSize":5,"totalItens":6,"totalPaginas":2}"""));
        });
        using var http = NovoHttp(handler);
        var texto = await ExecutarAsync(http, "3\n1\nn\na\nn\nn\n0\n0\n");
        Assert.Equal(new[] { 1, 2, 1, 2 }, paginas);
        Assert.Contains("Navegação inválida", texto);
        Assert.Contains("Registrado em:", texto);
    }

    [Fact]
    public async Task Menu_consume_API_real_com_seed_POST_detalhe_e_historico_vazio()
    {
        await using var factory = new ApiFactory();
        using var http = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<DemoSeeder>().ExecutarAsync(CancellationToken.None);
        var texto = await ExecutarAsync(http, "1\n2\n1\n2\n3\n3\n3\n4\n1\n2\n1\n2\n1\n0\n");
        Assert.Contains("DESATUALIZADA", texto);
        Assert.Contains("SEM_OBSERVACAO", texto);
        Assert.Contains("referência fictícia", texto);
        Assert.Contains("Coordenadas:", texto);
        Assert.Contains("Sem observações: ausência de informação", texto);
        Assert.Contains("HTTP 201", texto);
        Assert.Contains("RELATO_CIDADAO", texto);
        Assert.Contains("2026-11-18T15:00:00.0000000Z (UTC)", texto);
        var ponto = await new AguaNaRuaClient(http).ObterAsync(1, CancellationToken.None);
        Assert.Equal(OrigemObservacao.RELATO_CIDADAO, ponto.UltimaObservacao!.Origem);
    }

    [Fact]
    public async Task Menu_aceita_offset_apos_formato_invalido_e_persiste_o_mesmo_instante()
    {
        await using var factory = new ApiFactory();
        using var http = factory.CreateClient();
        var id = await factory.CriarPontoAsync();
        var texto = await ExecutarAsync(http, $"4\n{id}\n1\n2\nsem-fuso\n2\n2026-11-18T11:00:00-03:00\n0\n");
        Assert.Contains("Horário inválido", texto);
        Assert.Contains("HTTP 201", texto);
        var historico = await new AguaNaRuaClient(http).HistoricoAsync(id, 1, CancellationToken.None);
        var relato = Assert.Single(historico.Itens);
        Assert.Equal(FakeClock.Instante.AddHours(-1), relato.ObservadoEm);
        Assert.Equal(CondicaoRelatada.SEM_AGUA_ACUMULADA_OBSERVADA, relato.Condicao);
    }

    [Fact]
    public async Task Horario_futuro_e_devolvido_pelo_servidor_como_erro_de_validacao()
    {
        await using var factory = new ApiFactory();
        using var http = factory.CreateClient();
        var id = await factory.CriarPontoAsync();
        var texto = await ExecutarAsync(http, $"4\n{id}\n2\n2\n2026-11-19T15:00:00Z\n0\n");
        Assert.Contains("HTTP 400", texto);
        Assert.Contains("O horário observado não pode estar no futuro.", texto);
    }

    private static HttpClient NovoHttp(HttpMessageHandler handler, string url = "http://localhost:5080/")
        => new(handler, disposeHandler: false) { BaseAddress = new Uri(url) };

    private static async Task<string> ExecutarAsync(HttpClient http, string entrada, CancellationToken token = default)
    {
        using var leitor = new StringReader(entrada);
        using var escritor = new StringWriter();
        await new MenuConsole(new AguaNaRuaClient(http), leitor, escritor, new FakeClock()).ExecutarAsync(token);
        return escritor.ToString();
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string texto)
        => new(status) { Content = new StringContent(texto, Encoding.UTF8, "application/json") };

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        public int Chamadas { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Chamadas++;
            return responder(request, cancellationToken);
        }
    }

    private const string ObservacaoJson = """
        {"id":3,"pontoId":1,"condicao":"AGUA_ACUMULADA","origem":"RELATO_CIDADAO",
         "observadoEm":"2026-11-18T14:00:00Z","registradoEm":"2026-11-18T15:00:00Z"}
        """;
}
