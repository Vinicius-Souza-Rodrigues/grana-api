using Domain.Models;
using Grana.Domain.Notificacoes;
using Infraestructure.Seguranca;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Tests.Shared;

namespace Tests.Infrastructure;

public sealed class GeradorTokenTest
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 20, 14, 32, 10, TimeSpan.Zero);

    private readonly ConfiguracaoJwt _configuracao = new()
    {
        Chave = "chave-de-teste-com-mais-de-32-caracteres",
        Emissor = "grana-api",
        Audiencia = "grana-api"
    };

    private readonly Usuario _usuario = Usuario.CriacaoUsuario("Maria Silva", "maria@email.com", "senhaForte123", s => $"hash:{s}", new Notificacao());

    private TokenAcesso GerarToken() => new GeradorToken(Options.Create(_configuracao), new RelogioFixo(Agora)).Gerar(_usuario);

    [Fact]
    public void TokenExpiraEmDuasHoras()
    {
        var token = GerarToken();
        var jwt = new JsonWebToken(token.Token);

        Assert.Equal(Agora.UtcDateTime.AddHours(2), token.ExpiraEm);
        Assert.Equal(token.ExpiraEm, jwt.ValidTo);
        Assert.Equal(Agora.UtcDateTime, jwt.IssuedAt);
    }

    [Fact]
    public void TokenTemAsClaimsSubEmailEName()
    {
        var jwt = new JsonWebToken(GerarToken().Token);

        Assert.Equal(_usuario.Id.ToString(), jwt.Subject);
        Assert.Equal("maria@email.com", jwt.GetClaim(JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Maria Silva", jwt.GetClaim(JwtRegisteredClaimNames.Name).Value);
        Assert.Equal("grana-api", jwt.Issuer);
        Assert.Contains("grana-api", jwt.Audiences);
    }

    [Fact]
    public void TokenNaoLevaASenhaNemOHash()
    {
        var jwt = new JsonWebToken(GerarToken().Token);

        Assert.DoesNotContain(jwt.Claims, c => c.Value.Contains("senhaForte123"));
        Assert.DoesNotContain(jwt.Claims, c => c.Value == _usuario.SenhaHash);
    }

    [Fact]
    public async Task TokenEAssinadoComAChaveDaConfiguracao()
    {
        var token = GerarToken().Token;
        var manipulador = new JsonWebTokenHandler();

        var comChaveCerta = await manipulador.ValidateTokenAsync(token, ParametrosDeValidacao(_configuracao.ObterChaveAssinatura()));
        var comOutraChave = await manipulador.ValidateTokenAsync(token, ParametrosDeValidacao(new ConfiguracaoJwt { Chave = "outra-chave-de-teste-com-mais-de-32-caracteres" }.ObterChaveAssinatura()));

        Assert.True(comChaveCerta.IsValid);
        Assert.False(comOutraChave.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("curta")]
    [InlineData("1234567890123456789012345678901")]
    public void ConfiguracaoComChaveAusenteOuCurtaEInvalida(string chave)
    {
        _configuracao.Chave = chave;

        Assert.False(_configuracao.EstaValida());
    }

    [Fact]
    public void ConfiguracaoComChaveDeTrintaEDoisCaracteresEValida()
    {
        _configuracao.Chave = new string('a', 32);

        Assert.True(_configuracao.EstaValida());
    }

    private TokenValidationParameters ParametrosDeValidacao(SecurityKey chave) => new()
    {
        ValidIssuer = _configuracao.Emissor,
        ValidAudience = _configuracao.Audiencia,
        IssuerSigningKey = chave,
        ValidateLifetime = false
    };
}
