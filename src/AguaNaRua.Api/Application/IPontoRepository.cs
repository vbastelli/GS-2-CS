using AguaNaRua.Api.Domain;

namespace AguaNaRua.Api.Application;

public interface IPontoRepository
{
    Task<IReadOnlyList<PontoConsulta>> ListarAsync(CancellationToken cancellationToken);
    Task<PontoConsulta?> ObterAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExisteAsync(int id, CancellationToken cancellationToken);
    Task<PaginaResponse<ObservacaoResponse>> HistoricoAsync(int id, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<ObservacaoResponse?> ObterObservacaoAsync(int id, int observacaoId, CancellationToken cancellationToken);
    Task AdicionarAsync(Observacao observacao, CancellationToken cancellationToken);
}
