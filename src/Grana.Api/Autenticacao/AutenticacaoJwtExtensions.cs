using Infraestructure.Seguranca;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Grana.Api.Autenticacao;

public static class AutenticacaoJwtExtensions
{
    public const string MensagemTokenInvalido = "Token ausente, inválido ou expirado.";

    public static IServiceCollection AdicionarAutenticacaoJwt(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ConfiguracaoJwt>()
            .Bind(configuration.GetSection(ConfiguracaoJwt.Secao))
            .Validate(c => c.EstaValida(), $"Configure Jwt:Chave com pelo menos {ConfiguracaoJwt.TamanhoMinimoChave} caracteres, Jwt:Emissor e Jwt:Audiencia.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<ConfiguracaoJwt>>((opcoes, configuracaoJwt) =>
            {
                var jwt = configuracaoJwt.Value;

                opcoes.MapInboundClaims = false;
                opcoes.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Emissor,
                    ValidAudience = jwt.Audiencia,
                    IssuerSigningKey = jwt.ObterChaveAssinatura(),
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = JwtRegisteredClaimNames.Name
                };
                opcoes.Events = new JwtBearerEvents
                {
                    OnChallenge = async contexto =>
                    {
                        contexto.HandleResponse();
                        contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;

                        await contexto.Response.WriteAsJsonAsync(new
                        {
                            codigo = StatusCodes.Status401Unauthorized,
                            mensagem = "Não foi possível concluir a operação.",
                            erros = new[] { MensagemTokenInvalido }
                        });
                    }
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
