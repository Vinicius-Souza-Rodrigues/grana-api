using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Grana.Api.OpenApi;

public sealed class ExemplosOpenApiTransformer : IOpenApiOperationTransformer
{
    private const string TipoJson = "application/json";

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action)
            return Task.CompletedTask;

        if (!ExemplosOpenApi.PorAction.TryGetValue(action.ActionName, out var exemplo))
            return Task.CompletedTask;

        if (exemplo.Requisicao is not null && operation.RequestBody?.Content is not null)
        {
            foreach (var conteudo in operation.RequestBody.Content.Values)
                conteudo.Example = Converter(exemplo.Requisicao);
        }

        if (operation.Responses is null)
            return Task.CompletedTask;

        foreach (var (codigo, corpo) in exemplo.Respostas)
        {
            if (!operation.Responses.TryGetValue(codigo.ToString(), out var resposta) || resposta is not OpenApiResponse respostaOpenApi)
                continue;

            respostaOpenApi.Content ??= new Dictionary<string, OpenApiMediaType>();
            respostaOpenApi.Content[TipoJson] = new OpenApiMediaType { Example = Converter(corpo) };
        }

        return Task.CompletedTask;
    }

    private static JsonNode Converter(object exemplo) => JsonSerializer.SerializeToNode(exemplo);
}
