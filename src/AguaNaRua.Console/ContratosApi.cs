namespace AguaNaRua.ConsoleApp;

// Contratos do consumidor HTTP. Não dependem de entidades, serviços ou assemblies da API.
public enum CondicaoRelatada { SEM_AGUA_ACUMULADA_OBSERVADA, AGUA_ACUMULADA, ALAGAMENTO_RELATADO }
public enum OrigemObservacao { RELATO_CIDADAO, DEMONSTRACAO }
public enum EstadoAtualizacao { RECENTE, DESATUALIZADA, SEM_OBSERVACAO }

public sealed record ObservacaoDto
{
    public required int Id { get; init; }
    public required int PontoId { get; init; }
    public required CondicaoRelatada Condicao { get; init; }
    public required OrigemObservacao Origem { get; init; }
    public required DateTimeOffset ObservadoEm { get; init; }
    public required DateTimeOffset RegistradoEm { get; init; }
}

public sealed record PontoDto
{
    public required int Id { get; init; }
    public required string Nome { get; init; }
    public required string Bairro { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required bool Demonstracao { get; init; }
    public required EstadoAtualizacao StatusAtualizacao { get; init; }
    public required ObservacaoDto? UltimaObservacao { get; init; }
}

public sealed record PaginaObservacoesDto
{
    public required List<ObservacaoDto> Itens { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalItens { get; init; }
    public required int TotalPaginas { get; init; }
}

public sealed record RegistrarObservacaoDto(CondicaoRelatada Condicao, DateTimeOffset ObservadoEm);
