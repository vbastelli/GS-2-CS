using AguaNaRua.Api.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AguaNaRua.Api.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidacaoDominioException validacao) return false;
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [validacao.Campo] = [validacao.Message]
            })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dados inválidos.",
                Instance = httpContext.Request.Path
            }
        });
    }
}
