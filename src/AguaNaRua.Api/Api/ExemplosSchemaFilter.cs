using System.Text.Json.Nodes;
using AguaNaRua.Api.Application;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AguaNaRua.Api.Api;

public sealed class ExemplosSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema openApiSchema, SchemaFilterContext context)
    {
        if (openApiSchema is not OpenApiSchema schema) return;
        if (context.Type == typeof(CriarObservacaoRequest))
            schema.Example = JsonNode.Parse("""
                { "condicao": "AGUA_ACUMULADA", "observadoEm": "2026-11-18T12:00:00Z" }
                """);
        if (context.Type == typeof(ObservacaoResponse))
            schema.Example = JsonNode.Parse("""
                {
                  "id": 1, "pontoId": 1, "condicao": "AGUA_ACUMULADA", "origem": "DEMONSTRACAO",
                  "observadoEm": "2026-11-18T12:00:00Z", "registradoEm": "2026-11-18T12:30:00Z"
                }
                """);
        if (context.Type == typeof(PontoResponse))
            schema.Example = JsonNode.Parse("""
                {
                  "id": 3, "nome": "Trecho C — demonstração fictícia", "bairro": "Jardim Peri",
                  "latitude": -23.447, "longitude": -46.659, "demonstracao": true,
                  "statusAtualizacao": "SEM_OBSERVACAO", "ultimaObservacao": null
                }
                """);
    }
}
