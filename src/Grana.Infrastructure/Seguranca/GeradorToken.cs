using Application.Interfaces;
using Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infraestructure.Seguranca;

public sealed class GeradorToken(IOptions<ConfiguracaoJwt> opcoes, TimeProvider relogio) : IGeradorToken
{
    private readonly JsonWebTokenHandler _manipulador = new();

    public TokenAcesso Gerar(Usuario usuario)
    {
        var configuracao = opcoes.Value;
        var agora = DateTimeOffset.FromUnixTimeSeconds(relogio.GetUtcNow().ToUnixTimeSeconds()).UtcDateTime;
        var expiraEm = agora.AddHours(TokenAcesso.ValidadeEmHoras);

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = configuracao.Emissor,
            Audience = configuracao.Audiencia,
            IssuedAt = agora,
            NotBefore = agora,
            Expires = expiraEm,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = usuario.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = usuario.Email,
                [JwtRegisteredClaimNames.Name] = usuario.Nome
            },
            SigningCredentials = new SigningCredentials(configuracao.ObterChaveAssinatura(), SecurityAlgorithms.HmacSha256)
        };

        return new TokenAcesso(_manipulador.CreateToken(descritor), expiraEm);
    }
}
