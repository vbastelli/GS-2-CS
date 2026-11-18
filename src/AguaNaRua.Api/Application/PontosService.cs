using AguaNaRua.Api.Domain;

namespace AguaNaRua.Api.Application;

public sealed class PontosService(IPontoRepository repository, AtualizacaoPolicy atualizacao,
    TimeProvider relogio) : IPontosService
{
    public async Task<IReadOnlyList<PontoResponse>> ListarAsync(CancellationToken cancellationToken)
        => (await repository.ListarAsync(cancellationToken)).Select(Mapear).ToList();

    public async Task<PontoResponse?> ObterAsync(int id, CancellationToken cancellationToken)
    {
        var ponto = await repository.ObterAsync(id, cancellationToken);
        return ponto is null ? null : Mapear(ponto);
    }

    public async Task<PaginaResponse<ObservacaoResponse>?> HistoricoAsync(int id, int page,
        int pageSize, CancellationToken cancellationToken)
    {
        // Protege o caso de uso também quando chamado sem HTTP.
        if (page < 1 || page > int.MaxValue / 100 || pageSize is < 1 or > 100)
            throw new ValidacaoDominioException("paginacao", "page deve ser de 1 a 21474836 e pageSize de 1 a 100.");
        if (!await repository.ExisteAsync(id, cancellationToken)) return null;
        return await repository.HistoricoAsync(id, page, pageSize, cancellationToken);
    }

    public Task<ObservacaoResponse?> ObterObservacaoAsync(int id, int observacaoId,
        CancellationToken cancellationToken)
        => repository.ObterObservacaoAsync(id, observacaoId, cancellationToken);

    public async Task<ObservacaoResponse?> RegistrarAsync(int id, CriarObservacaoRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Condicao is null || request.ObservadoEm is null)
            throw new ValidacaoDominioException("observacao", "condicao e observadoEm são obrigatórios.");

        var observacao = Observacao.Criar(id, request.Condicao.Value, OrigemObservacao.RELATO_CIDADAO,
            request.ObservadoEm.Value, relogio.GetUtcNow());
        if (!await repository.ExisteAsync(id, cancellationToken)) return null;
        await repository.AdicionarAsync(observacao, cancellationToken);
        return new ObservacaoResponse(observacao.Id, observacao.PontoMonitoradoId, observacao.Condicao,
            observacao.Origem, observacao.ObservadoEm, observacao.RegistradoEm);
    }

    private PontoResponse Mapear(PontoConsulta ponto) => new(ponto.Id, ponto.Nome, ponto.Bairro,
        ponto.Latitude, ponto.Longitude, ponto.Demonstracao,
        atualizacao.Classificar(ponto.UltimaObservacao?.ObservadoEm), ponto.UltimaObservacao);
}
