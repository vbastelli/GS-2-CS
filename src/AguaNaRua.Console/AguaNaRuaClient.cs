using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AguaNaRua.ConsoleApp;

public sealed class RespostaApiException(string mensagem, HttpStatusCode statusCode) : Exception(mensagem)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public sealed class AguaNaRuaClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false), new HorarioJsonConverter() }
    };

    public Uri Endereco => http.BaseAddress ?? throw new InvalidOperationException("Configure a URL da API.");
    public TimeSpan Timeout => http.Timeout;

    public Task<List<PontoDto>> ListarAsync(CancellationToken cancellationToken)
        => EnviarAsync<List<PontoDto>>(HttpMethod.Get, "api/pontos", HttpStatusCode.OK, null, cancellationToken);

    public Task<PontoDto> ObterAsync(int id, CancellationToken cancellationToken)
        => EnviarAsync<PontoDto>(HttpMethod.Get, $"api/pontos/{id}", HttpStatusCode.OK, null, cancellationToken);

    public Task<PaginaObservacoesDto> HistoricoAsync(int id, int page, CancellationToken cancellationToken)
        => EnviarAsync<PaginaObservacoesDto>(HttpMethod.Get, $"api/pontos/{id}/observacoes?page={page}&pageSize=5",
            HttpStatusCode.OK, null, cancellationToken);

    public Task<ObservacaoDto> RegistrarAsync(int id, RegistrarObservacaoDto observacao, CancellationToken cancellationToken)
        => EnviarAsync<ObservacaoDto>(HttpMethod.Post, $"api/pontos/{id}/observacoes", HttpStatusCode.Created,
            observacao, cancellationToken);

    private async Task<T> EnviarAsync<T>(HttpMethod metodo, string caminho, HttpStatusCode esperado,
        RegistrarObservacaoDto? corpo, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(metodo, caminho);
        if (corpo is not null) request.Content = JsonContent.Create(corpo, options: Json);
        using var response = await http.SendAsync(request, cancellationToken);
        var texto = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.StatusCode != esperado)
            throw new RespostaApiException(DescreverErro(response.StatusCode, texto), response.StatusCode);

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType != "application/json" && !(mediaType?.EndsWith("+json", StringComparison.OrdinalIgnoreCase) ?? false))
            throw new RespostaApiException("Resposta inesperada: a API não retornou conteúdo JSON.", response.StatusCode);
        try
        {
            var dados = JsonSerializer.Deserialize<T>(texto, Json);
            if (dados is null || !ContratoValido(dados)) throw new JsonException();
            return dados;
        }
        catch (JsonException)
        {
            throw new RespostaApiException("A API retornou JSON inválido ou incompatível com o contrato esperado.", response.StatusCode);
        }
    }

    // Verifica o formato recebido, sem recalcular recência ou regras de negócio do servidor.
    private static bool ContratoValido(object dados) => dados switch
    {
        PontoDto p => p.Id > 0 && !string.IsNullOrWhiteSpace(p.Nome) && !string.IsNullOrWhiteSpace(p.Bairro)
            && Enum.IsDefined(p.StatusAtualizacao) && double.IsFinite(p.Latitude) && double.IsFinite(p.Longitude)
            && (p.UltimaObservacao is null
                ? p.StatusAtualizacao == EstadoAtualizacao.SEM_OBSERVACAO
                : ContratoValido(p.UltimaObservacao)),
        ObservacaoDto o => o.Id > 0 && o.PontoId > 0 && Enum.IsDefined(o.Condicao) && Enum.IsDefined(o.Origem)
            && o.ObservadoEm != default && o.RegistradoEm != default,
        List<PontoDto> pontos => pontos.All(p => p is not null && ContratoValido(p)),
        PaginaObservacoesDto p => p.Itens is not null && p.Page > 0 && p.PageSize > 0
            && p.TotalItens >= 0 && p.TotalPaginas >= 0 && p.Itens.All(o => o is not null && ContratoValido(o)),
        _ => false
    };

    private static string DescreverErro(HttpStatusCode status, string texto)
    {
        var mensagem = status switch
        {
            HttpStatusCode.BadRequest => "Dados recusados pela API.",
            HttpStatusCode.NotFound => "Ponto ou observação não encontrado.",
            _ => "Resposta inesperada da API."
        };
        var linhas = new List<string> { $"HTTP {(int)status} — {mensagem}" };
        try
        {
            using var json = JsonDocument.Parse(texto);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return linhas[0];
            foreach (var campo in new[] { "title", "detail" })
                if (root.TryGetProperty(campo, out var valor) && valor.ValueKind == JsonValueKind.String)
                    linhas.Add(valor.GetString()!);
            if (root.TryGetProperty("errors", out var erros) && erros.ValueKind == JsonValueKind.Object)
                foreach (var erro in erros.EnumerateObject())
                    if (erro.Value.ValueKind == JsonValueKind.Array)
                        foreach (var item in erro.Value.EnumerateArray())
                            if (item.ValueKind == JsonValueKind.String) linhas.Add($"{erro.Name}: {item.GetString()}");
        }
        catch (JsonException) { /* Mantém o status e a mensagem compreensível para corpos não JSON. */ }
        return string.Join(Environment.NewLine, linhas);
    }
}
