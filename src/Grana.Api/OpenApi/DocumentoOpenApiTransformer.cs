using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Grana.Api.OpenApi;

public sealed class DocumentoOpenApiTransformer : IOpenApiDocumentTransformer
{
    public const string EsquemaBearer = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info.Title = "Grana API";
        document.Info.Description = "API de finanças pessoais: contas, categorias, lançamentos, transferências, orçamento, resumo mensal e extrato.";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[EsquemaBearer] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token JWT obtido no login."
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(EsquemaBearer, document)] = []
        });

        return Task.CompletedTask;
    }
}
