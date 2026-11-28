using System.Text.Json;
using Grana.Api.Autenticacao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Grana.Api.OpenApi;

public sealed class SegurancaOpenApiTransformer : IOpenApiOperationTransformer
{
    private const string TipoJson = "application/json";
    private const string CodigoNaoAutorizado = "401";

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            return Task.CompletedTask;

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(DocumentoOpenApiTransformer.EsquemaBearer, context.Document)] = []
        });

        operation.Responses ??= new OpenApiResponses();
        operation.Responses[CodigoNaoAutorizado] = new OpenApiResponse
        {
            Description = "Unauthorized",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [TipoJson] = new()
                {
                    Example = JsonSerializer.SerializeToNode(new
                    {
                        codigo = 401,
                        mensagem = "Não foi possível concluir a operação.",
                        erros = new[] { AutenticacaoJwtExtensions.MensagemTokenInvalido }
                    })
                }
            }
        };

        return Task.CompletedTask;
    }
}
