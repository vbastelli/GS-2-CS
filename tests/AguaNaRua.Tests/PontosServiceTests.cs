using AguaNaRua.Api.Application;
using AguaNaRua.Api.Domain;
using Microsoft.Extensions.Options;

namespace AguaNaRua.Tests;

public sealed class PontosServiceTests
{
    private static PontosService Criar(RepositorySubstituto repository) => new(repository,
        new AtualizacaoPolicy(new FakeClock(), Options.Create(new AtualizacaoOptions())), new FakeClock());

    [Fact]
    public async Task Registro_define_origem_relogio_e_propaga_cancelamento()
    {
        var repo = new RepositorySubstituto();
        using var cts = new CancellationTokenSource();
        var resposta = await Criar(repo).RegistrarAsync(1, new CriarObservacaoRequest
        {
            Condicao = CondicaoRelatada.AGUA_ACUMULADA,
            ObservadoEm = FakeClock.Instante.AddHours(-2)
        }, cts.Token);

        Assert.NotNull(resposta);
        Assert.Equal(OrigemObservacao.RELATO_CIDADAO, resposta.Origem);
        Assert.Equal(FakeClock.Instante, resposta.RegistradoEm);
        Assert.NotNull(repo.Adicionada);
        Assert.Equal(cts.Token, repo.TokenRecebido);
    }

    [Fact]
    public async Task Ponto_inexistente_nao_persiste_relato()
    {
        var repo = new RepositorySubstituto { Existe = false };
        Assert.Null(await Criar(repo).RegistrarAsync(99, new CriarObservacaoRequest
        {
            Condicao = CondicaoRelatada.AGUA_ACUMULADA,
            ObservadoEm = FakeClock.Instante
        }, CancellationToken.None));
        Assert.Null(repo.Adicionada);
    }

    [Fact]
    public async Task Recencia_usa_horario_observado_mesmo_com_registro_novo()
    {
        var repo = new RepositorySubstituto
        {
            Ponto = new PontoConsulta(1, "A", "Jardim Peri", 0, 0, true,
                new ObservacaoResponse(1, 1, CondicaoRelatada.AGUA_ACUMULADA, OrigemObservacao.RELATO_CIDADAO,
                    FakeClock.Instante.AddHours(-48), FakeClock.Instante))
        };
        var ponto = await Criar(repo).ObterAsync(1, CancellationToken.None);
        Assert.Equal(EstadoAtualizacao.DESATUALIZADA, ponto!.StatusAtualizacao);
    }

    [Fact]
    public async Task Ponto_existente_sem_relato_tem_ultima_nula()
    {
        var repo = new RepositorySubstituto { Ponto = new PontoConsulta(1, "A", "Jardim Peri", 0, 0, true, null) };
        var ponto = await Criar(repo).ObterAsync(1, CancellationToken.None);
        Assert.Equal(EstadoAtualizacao.SEM_OBSERVACAO, ponto!.StatusAtualizacao);
        Assert.Null(ponto.UltimaObservacao);
    }

    [Fact]
    public async Task Horario_futuro_nao_chega_a_persistencia()
    {
        var repo = new RepositorySubstituto();
        await Assert.ThrowsAsync<ValidacaoDominioException>(() => Criar(repo).RegistrarAsync(1,
            new CriarObservacaoRequest
            {
                Condicao = CondicaoRelatada.AGUA_ACUMULADA,
                ObservadoEm = FakeClock.Instante.AddSeconds(1)
            }, CancellationToken.None));
        Assert.Null(repo.Adicionada);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public async Task Paginacao_invalida_e_rejeitada_sem_HTTP(int page, int pageSize)
        => await Assert.ThrowsAsync<ValidacaoDominioException>(() => Criar(new RepositorySubstituto())
            .HistoricoAsync(1, page, pageSize, CancellationToken.None));

    private sealed class RepositorySubstituto : IPontoRepository
    {
        public bool Existe { get; init; } = true;
        public PontoConsulta? Ponto { get; init; }
        public Observacao? Adicionada { get; private set; }
        public CancellationToken TokenRecebido { get; private set; }
        public Task<bool> ExisteAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Existe);
        public Task<PontoConsulta?> ObterAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Ponto);
        public Task<IReadOnlyList<PontoConsulta>> ListarAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<PontoConsulta>>(Ponto is null ? [] : [Ponto]);
        public Task<PaginaResponse<ObservacaoResponse>> HistoricoAsync(int id, int page, int pageSize,
            CancellationToken cancellationToken)
            => Task.FromResult(new PaginaResponse<ObservacaoResponse>([], page, pageSize, 0, 0));
        public Task<ObservacaoResponse?> ObterObservacaoAsync(int id, int observacaoId, CancellationToken cancellationToken)
            => Task.FromResult<ObservacaoResponse?>(null);
        public Task AdicionarAsync(Observacao observacao, CancellationToken cancellationToken)
        {
            Adicionada = observacao;
            TokenRecebido = cancellationToken;
            return Task.CompletedTask;
        }
    }
}
