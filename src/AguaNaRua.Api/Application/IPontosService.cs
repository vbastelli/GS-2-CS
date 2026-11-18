namespace AguaNaRua.Api.Application;

public interface IPontosService
{
    Task<IReadOnlyList<PontoResponse>> ListarAsync(CancellationToken cancellationToken);
    Task<PontoResponse?> ObterAsync(int id, CancellationToken cancellationToken);
    Task<PaginaResponse<ObservacaoResponse>?> HistoricoAsync(int id, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<ObservacaoResponse?> ObterObservacaoAsync(int id, int observacaoId, CancellationToken cancellationToken);
    Task<ObservacaoResponse?> RegistrarAsync(int id, CriarObservacaoRequest request,
        CancellationToken cancellationToken);
}
