using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AguaNaRua.Api.Application;
using AguaNaRua.Api.Domain;
using AguaNaRua.Api.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AguaNaRua.Tests;

public sealed class ApiIntegrationTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Post_location_get_e_reabertura_do_arquivo_preservam_relato_em_UTC()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var id = await factory.CriarPontoAsync();
        using var post = await client.PostAsJsonAsync($"/api/pontos/{id}/observacoes", new
        {
            condicao = "AGUA_ACUMULADA",
            observadoEm = "2026-11-18T11:00:00-03:00"
        });
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        Assert.NotNull(post.Headers.Location);
        var criado = await post.Content.ReadFromJsonAsync<ObservacaoResponse>(Json);
        var consultado = await client.GetFromJsonAsync<ObservacaoResponse>(post.Headers.Location, Json);
        Assert.Equal(criado, consultado);
        Assert.Equal(OrigemObservacao.RELATO_CIDADAO, criado!.Origem);
        Assert.Equal(FakeClock.Instante, criado.RegistradoEm);
        Assert.Equal(TimeSpan.Zero, criado.ObservadoEm.Offset);

        // Nova conexão/contexto sobre arquivo real, sem depender do ChangeTracker do POST.
        var options = new DbContextOptionsBuilder<AguaDbContext>()
            .UseSqlite($"Data Source={factory.DatabasePath};Foreign Keys=True;Pooling=False").Options;
        await using var db = new AguaDbContext(options);
        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", db.Database.ProviderName);
        var persistida = await db.Observacoes.AsNoTracking().SingleAsync();
        Assert.Equal(criado.Id, persistida.Id);
        Assert.Equal(criado.ObservadoEm, persistida.ObservadoEm);
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
        await using var conexao = new SqliteConnection($"Data Source={factory.DatabasePath};Pooling=False");
        await conexao.OpenAsync();
        await using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT typeof(ObservadoEm) FROM Observacoes LIMIT 1";
        Assert.Equal("integer", await comando.ExecuteScalarAsync());
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"/api/pontos/{id + 1}/observacoes/{criado.Id}")).StatusCode);
    }

    [Fact]
    public async Task Ultima_e_historico_ordenam_no_SQLite_por_observado_registrado_e_id()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var pontoId = await factory.CriarPontoAsync();
        int esperado;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AguaDbContext>();
            // Inserção fora da ordem cronológica; offsets diferentes representam o mesmo instante.
            db.Observacoes.AddRange(
                Observacao.Criar(pontoId, CondicaoRelatada.AGUA_ACUMULADA, OrigemObservacao.DEMONSTRACAO,
                    FakeClock.Instante.AddHours(-2), FakeClock.Instante.AddMinutes(-1)),
                Observacao.Criar(pontoId, CondicaoRelatada.ALAGAMENTO_RELATADO, OrigemObservacao.DEMONSTRACAO,
                    FakeClock.Instante.AddHours(-1).ToOffset(TimeSpan.FromHours(7)), FakeClock.Instante.AddMinutes(-2)),
                Observacao.Criar(pontoId, CondicaoRelatada.SEM_AGUA_ACUMULADA_OBSERVADA, OrigemObservacao.DEMONSTRACAO,
                    FakeClock.Instante.AddHours(-1).ToOffset(TimeSpan.FromHours(-3)), FakeClock.Instante.AddMinutes(-1)));
            await db.SaveChangesAsync();
            var desempate = Observacao.Criar(pontoId, CondicaoRelatada.AGUA_ACUMULADA, OrigemObservacao.DEMONSTRACAO,
                FakeClock.Instante.AddHours(-1), FakeClock.Instante.AddMinutes(-1));
            db.Observacoes.Add(desempate);
            await db.SaveChangesAsync();
            esperado = desempate.Id;
        }

        factory.Counter.Comandos.Clear();
        var ponto = await client.GetFromJsonAsync<PontoResponse>($"/api/pontos/{pontoId}", Json);
        Assert.Equal(esperado, ponto!.UltimaObservacao!.Id);
        var pagina = await client.GetFromJsonAsync<PaginaResponse<ObservacaoResponse>>(
            $"/api/pontos/{pontoId}/observacoes?page=1&pageSize=2", Json);
        Assert.Equal(4, pagina!.TotalItens);
        Assert.Equal(2, pagina.TotalPaginas);
        Assert.Equal(2, pagina.Itens.Count);
        Assert.Equal(esperado, pagina.Itens[0].Id);
        var segunda = await client.GetFromJsonAsync<PaginaResponse<ObservacaoResponse>>(
            $"/api/pontos/{pontoId}/observacoes?page=2&pageSize=2", Json);
        Assert.Empty(pagina.Itens.Select(x => x.Id).Intersect(segunda!.Itens.Select(x => x.Id)));
        Assert.Contains(factory.Counter.Comandos, sql => sql.Contains("ORDER BY") && sql.Contains("LIMIT"));
    }

    [Fact]
    public async Task Seed_e_idempotente_mostra_tres_estados_e_listagem_nao_faz_N_mais_1()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DemoSeeder>();
            await seeder.ExecutarAsync(CancellationToken.None);
            await seeder.ExecutarAsync(CancellationToken.None);
            var db = scope.ServiceProvider.GetRequiredService<AguaDbContext>();
            Assert.Equal(3, await db.Pontos.CountAsync());
            Assert.Equal(2, await db.Observacoes.CountAsync());
            // Mais pontos tornam visível uma eventual regressão para consulta por ponto.
            db.Pontos.AddRange(Enumerable.Range(0, 12).Select(i =>
                PontoMonitorado.Criar($"Fictício {i}", "Jardim Peri", 0, 0, true)));
            await db.SaveChangesAsync();
        }
        factory.Counter.Comandos.Clear();
        var pontos = await client.GetFromJsonAsync<List<PontoResponse>>("/api/pontos", Json);
        Assert.Equal(15, pontos!.Count);
        Assert.Equal(2, factory.Counter.Comandos.Count);
        Assert.All(pontos, p => Assert.True(p.Demonstracao));
        Assert.Equal(EstadoAtualizacao.RECENTE, pontos[0].StatusAtualizacao);
        Assert.Equal(EstadoAtualizacao.DESATUALIZADA, pontos[1].StatusAtualizacao);
        Assert.Equal(EstadoAtualizacao.SEM_OBSERVACAO, pontos[2].StatusAtualizacao);
        Assert.Equal(OrigemObservacao.DEMONSTRACAO, pontos[0].UltimaObservacao!.Origem);

        var sem = await client.GetFromJsonAsync<PontoResponse>($"/api/pontos/{pontos[2].Id}", Json);
        Assert.Null(sem!.UltimaObservacao);
        var historico = await client.GetFromJsonAsync<PaginaResponse<ObservacaoResponse>>(
            $"/api/pontos/{sem.Id}/observacoes", Json);
        Assert.Empty(historico!.Itens);
        Assert.Equal(0, historico.TotalItens);
        Assert.Equal(0, historico.TotalPaginas);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"condicao\":\"OUTRA\",\"observadoEm\":\"2026-11-18T12:00:00Z\"}")]
    [InlineData("{\"condicao\":1,\"observadoEm\":\"2026-11-18T12:00:00Z\"}")]
    [InlineData("{\"condicao\":\"AGUA_ACUMULADA\",\"observadoEm\":\"2026-11-18T15:00:01Z\"}")]
    [InlineData("{\"condicao\":\"AGUA_ACUMULADA\",\"observadoEm\":\"2026-11-18T12:00:00\"}")]
    [InlineData("{\"condicao\":\"AGUA_ACUMULADA\",\"observadoEm\":\"2026-11-18T12:00:00Z\",\"origem\":\"DEMONSTRACAO\"}")]
    [InlineData("{\"condicao\":\"AGUA_ACUMULADA\",\"observadoEm\":\"2026-11-18T12:00:00Z\",\"registradoEm\":\"2026-11-18T12:00:00Z\"}")]
    [InlineData("{\"condicao\":\"AGUA_ACUMULADA\",\"observadoEm\":\"2026-11-18T12:00:00Z\",\"nome\":\"Pessoa\"}")]
    [InlineData("{")]
    public async Task Corpo_invalido_retorna_400_ProblemDetails_sem_persistir(string corpo)
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var id = await factory.CriarPontoAsync();
        using var resposta = await client.PostAsync($"/api/pontos/{id}/observacoes",
            new StringContent(corpo, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType!.MediaType);
        using var problem = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("errors", out _));
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Equal(0, await scope.ServiceProvider.GetRequiredService<AguaDbContext>().Observacoes.CountAsync());
    }

    [Theory]
    [InlineData("/api/pontos/0", 400)]
    [InlineData("/api/pontos/abc", 400)]
    [InlineData("/api/pontos/-1", 400)]
    [InlineData("/api/pontos/1/observacoes?page=0", 400)]
    [InlineData("/api/pontos/1/observacoes?pageSize=101", 400)]
    [InlineData("/api/pontos/1/observacoes?pageSize=0", 400)]
    [InlineData("/api/pontos/1/observacoes?page=2147483647", 400)]
    [InlineData("/api/pontos/999", 404)]
    [InlineData("/api/pontos/999/observacoes", 404)]
    [InlineData("/api/pontos/999/observacoes/1", 404)]
    public async Task Identificadores_paginacao_e_recursos_inexistentes_tem_erro_padrao(string url, int status)
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var resposta = await client.GetAsync(url);
        Assert.Equal(status, (int)resposta.StatusCode);
        Assert.Equal("application/problem+json", resposta.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Post_em_ponto_inexistente_retorna_404()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var resposta = await client.PostAsJsonAsync("/api/pontos/999/observacoes", new
        {
            condicao = "ALAGAMENTO_RELATADO",
            observadoEm = "2026-11-18T12:00:00Z"
        });
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task FK_relacional_rejeita_observacao_sem_ponto()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AguaDbContext>();
        db.Observacoes.Add(Observacao.Criar(999, CondicaoRelatada.AGUA_ACUMULADA,
            OrigemObservacao.RELATO_CIDADAO, FakeClock.Instante, FakeClock.Instante));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task OpenAPI_documenta_cinco_operacoes_enums_exemplos_e_respostas()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        using var doc = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var root = doc.RootElement;
        var paths = root.GetProperty("paths");
        Assert.Equal(4, paths.EnumerateObject().Count());
        Assert.True(paths.GetProperty("/api/pontos/{id}/observacoes").TryGetProperty("post", out var post));
        Assert.True(post.GetProperty("responses").TryGetProperty("201", out _));
        Assert.True(post.GetProperty("responses").TryGetProperty("400", out _));
        Assert.True(post.GetProperty("responses").TryGetProperty("404", out _));
        Assert.True(post.GetProperty("responses").GetProperty("400").GetProperty("content")
            .TryGetProperty("application/problem+json", out _));
        Assert.True(post.GetProperty("requestBody").GetProperty("required").GetBoolean());
        var schemas = root.GetProperty("components").GetProperty("schemas");
        var condicoes = schemas.GetProperty("CondicaoRelatada").GetProperty("enum");
        Assert.Equal(3, condicoes.GetArrayLength());
        var request = schemas.GetProperty("CriarObservacaoRequest").GetProperty("example");
        Assert.Equal("AGUA_ACUMULADA", request.GetProperty("condicao").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/swagger/index.html")).StatusCode);
    }
}
