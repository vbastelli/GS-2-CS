using System.ComponentModel.DataAnnotations;
using AguaNaRua.Api.Application;
using Microsoft.AspNetCore.Mvc;

namespace AguaNaRua.Api.Api;

[ApiController]
[Route("api/pontos")]
public sealed class PontosController(IPontosService service) : ControllerBase
{
    /// <summary>Lista pontos e a última condição relatada, com origem e estado de atualização.</summary>
    /// <remarks>demonstracao=true identifica referências fictícias. Ausência de relato não indica condição de passagem.</remarks>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PontoResponse>>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<IReadOnlyList<PontoResponse>>> Listar(CancellationToken cancellationToken)
        => Ok(await service.ListarAsync(cancellationToken));

    /// <summary>Consulta um ponto. Sem relatos: ultimaObservacao=null e statusAtualizacao=SEM_OBSERVACAO.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType<PontoResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PontoResponse>> Obter([Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var ponto = await service.ObterAsync(id, cancellationToken);
        return ponto is null ? NaoEncontrado() : Ok(ponto);
    }

    /// <summary>Histórico paginado, ordenado por observadoEm, registradoEm e id decrescentes.</summary>
    /// <param name="id">Identificador positivo do ponto.</param>
    /// <param name="page">Página a partir de 1 (máximo 21474836).</param>
    /// <param name="pageSize">Quantidade de 1 a 100, padrão 20.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    [HttpGet("{id}/observacoes")]
    [ProducesResponseType<PaginaResponse<ObservacaoResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PaginaResponse<ObservacaoResponse>>> Historico(
        [Range(1, int.MaxValue)] int id, CancellationToken cancellationToken,
        [FromQuery, Range(1, int.MaxValue / 100)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
    {
        var pagina = await service.HistoricoAsync(id, page, pageSize, cancellationToken);
        return pagina is null ? NaoEncontrado() : Ok(pagina);
    }

    /// <summary>Consulta um relato pertencente ao ponto indicado.</summary>
    [HttpGet("{id}/observacoes/{observacaoId}", Name = nameof(ObterObservacao))]
    [ProducesResponseType<ObservacaoResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ObservacaoResponse>> ObterObservacao(
        [Range(1, int.MaxValue)] int id, [Range(1, int.MaxValue)] int observacaoId,
        CancellationToken cancellationToken)
    {
        var observacao = await service.ObterObservacaoAsync(id, observacaoId, cancellationToken);
        return observacao is null ? NaoEncontrado() : Ok(observacao);
    }

    /// <summary>Registra condição relatada. Origem e horário de registro são preenchidos pelo servidor.</summary>
    /// <remarks>
    /// Corpo de exemplo: { "condicao": "AGUA_ACUMULADA", "observadoEm": "2026-11-18T12:00:00Z" }.
    /// Envie um instante passado com Z ou offset. Campos adicionais são rejeitados.
    /// A origem será RELATO_CIDADAO. Não envie nomes, contatos ou texto livre.
    /// </remarks>
    [HttpPost("{id}/observacoes")]
    [Consumes("application/json")]
    [ProducesResponseType<ObservacaoResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ObservacaoResponse>> Registrar([Range(1, int.MaxValue)] int id,
        [FromBody, Required] CriarObservacaoRequest request, CancellationToken cancellationToken)
    {
        var observacao = await service.RegistrarAsync(id, request, cancellationToken);
        return observacao is null ? NaoEncontrado() : CreatedAtRoute(nameof(ObterObservacao),
            new { id, observacaoId = observacao.Id }, observacao);
    }

    private ObjectResult NaoEncontrado() => Problem(statusCode: StatusCodes.Status404NotFound,
        title: "Recurso não encontrado.", detail: "O ponto ou a observação não foi encontrado neste caminho.");
}
