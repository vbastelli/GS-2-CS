using System.ComponentModel.DataAnnotations;
using AguaNaRua.Api.Domain;

namespace AguaNaRua.Api.Application;

public sealed record CriarObservacaoRequest
{
    /// <summary>Condição relatada, sem inferência de segurança de passagem.</summary>
    /// <example>AGUA_ACUMULADA</example>
    [Required]
    public CondicaoRelatada? Condicao { get; init; }

    /// <summary>Instante observado em ISO 8601, com Z ou offset. Não pode estar no futuro.</summary>
    /// <example>2026-11-18T12:00:00Z</example>
    [Required]
    public DateTimeOffset? ObservadoEm { get; init; }
}

public enum EstadoAtualizacao { RECENTE, DESATUALIZADA, SEM_OBSERVACAO }

public sealed record ObservacaoResponse(int Id, int PontoId, CondicaoRelatada Condicao,
    OrigemObservacao Origem, DateTimeOffset ObservadoEm, DateTimeOffset RegistradoEm);

public sealed record PontoResponse(int Id, string Nome, string Bairro, double Latitude,
    double Longitude, bool Demonstracao, EstadoAtualizacao StatusAtualizacao,
    ObservacaoResponse? UltimaObservacao);

public sealed record PaginaResponse<T>(IReadOnlyList<T> Itens, int Page, int PageSize,
    int TotalItens, int TotalPaginas);

// Modelo de leitura interno: o caso de uso calcula o estado com o relógio injetado.
public sealed record PontoConsulta(int Id, string Nome, string Bairro, double Latitude,
    double Longitude, bool Demonstracao, ObservacaoResponse? UltimaObservacao);
