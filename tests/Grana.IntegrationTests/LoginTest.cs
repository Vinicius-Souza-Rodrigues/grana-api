using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Domain.Models;
using Grana.Domain.Notificacoes;
using Infraestructure.Seguranca;
using IntegrationTests.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace IntegrationTests;

public sealed class LoginTest(GranaApiFactory factory) : TesteDeIntegracao(factory)
{
    private const string Email = "maria@email.com";
    private const string MensagemCredenciaisInvalidas = "E-mail ou senha inválidos.";
    private const string MensagemTokenInvalido = "Token ausente, inválido ou expirado.";

    [Fact]
    public async Task CredenciaisCorretasGeram200ComToken()
    {
        using var cliente = Factory.CreateClient();
        var registro = await cliente.PostAsJsonAsync("api/auth/registrar", new { nome = "Maria Silva", email = Email, senha = SenhaPadrao });
        var idUsuario = (await LerDados(registro)).GetProperty("id").GetString();

        var resposta = await cliente.PostAsJsonAsync("api/auth/login", new { email = "MARIA@email.com", senha = SenhaPadrao });
        var dados = await LerDados(resposta);
        var jwt = new JsonWebToken(dados.GetProperty("token").GetString());

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(idUsuario, jwt.Subject);
        Assert.Equal(Email, jwt.GetClaim(JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Maria Silva", jwt.GetClaim(JwtRegisteredClaimNames.Name).Value);
        Assert.Equal(jwt.ValidTo, dados.GetProperty("expiraEm").GetDateTime());
        Assert.Equal(TimeSpan.FromHours(2), jwt.ValidTo - jwt.IssuedAt);
    }

    [Fact]
    public async Task SenhaErradaEEmailInexistenteGeram401ComAMesmaMensagem()
    {
        using var cliente = Factory.CreateClient();
        await cliente.PostAsJsonAsync("api/auth/registrar", new { nome = "Maria Silva", email = Email, senha = SenhaPadrao });

        var senhaErrada = await cliente.PostAsJsonAsync("api/auth/login", new { email = Email, senha = "senhaErrada123" });
        var emailInexistente = await cliente.PostAsJsonAsync("api/auth/login", new { email = "joao@email.com", senha = SenhaPadrao });
        var corpoSenhaErrada = await senhaErrada.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, senhaErrada.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, emailInexistente.StatusCode);
        Assert.Equal(MensagemCredenciaisInvalidas, (await LerJson(senhaErrada)).GetProperty("erros")[0].GetString());
        Assert.Equal(corpoSenhaErrada, await emailInexistente.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RotaProtegidaSemTokenGera401()
    {
        using var cliente = Factory.CreateClient();

        var resposta = await cliente.GetAsync("api/contas");
        var json = await LerJson(resposta);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal(401, json.GetProperty("codigo").GetInt32());
        Assert.Equal(MensagemTokenInvalido, json.GetProperty("erros")[0].GetString());
    }

    [Fact]
    public async Task TokenExpiradoEmRotaProtegidaGera401()
    {
        var configuracaoJwt = Factory.Services.GetRequiredService<IOptions<ConfiguracaoJwt>>();
        var usuario = Usuario.CriacaoUsuario("Maria Silva", Email, SenhaPadrao, s => s, new Notificacao());
        var tokenExpirado = new GeradorToken(configuracaoJwt, new RelogioFixo(DateTimeOffset.UtcNow.AddHours(-2).AddSeconds(-5))).Gerar(usuario);

        using var cliente = Factory.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenExpirado.Token);

        var resposta = await cliente.GetAsync("api/contas");

        Assert.True(tokenExpirado.ExpiraEm < DateTime.UtcNow);
        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal(MensagemTokenInvalido, (await LerJson(resposta)).GetProperty("erros")[0].GetString());
    }

    [Fact]
    public async Task TokenAssinadoComOutraChaveGera401()
    {
        var outraConfiguracao = Options.Create(new ConfiguracaoJwt { Chave = "outra-chave-que-a-api-nao-conhece-123456", Emissor = "grana-api", Audiencia = "grana-api" });
        var usuario = Usuario.CriacaoUsuario("Maria Silva", Email, SenhaPadrao, s => s, new Notificacao());
        var tokenFalso = new GeradorToken(outraConfiguracao, TimeProvider.System).Gerar(usuario);

        using var cliente = Factory.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenFalso.Token);

        var resposta = await cliente.GetAsync("api/contas");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task TokenValidoAcessaRotaProtegida()
    {
        var resposta = await Cliente.GetAsync("api/contas");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Theory]
    [InlineData("health")]
    [InlineData("openapi/v1.json")]
    [InlineData("scalar")]
    public async Task RotasPublicasNaoPedemToken(string rota)
    {
        using var cliente = Factory.CreateClient();

        var resposta = await cliente.GetAsync(rota);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Theory]
    [InlineData("api/contas")]
    [InlineData("api/categorias")]
    [InlineData("api/lancamentos")]
    [InlineData("api/transferencias")]
    [InlineData("api/orcamentos")]
    [InlineData("api/resumo")]
    [InlineData("api/contas/3f2b8c1e-6a4d-4e1b-9c7a-2d5e8f1a4b6c/extrato")]
    public async Task RecursosExigemToken(string rota)
    {
        using var cliente = Factory.CreateClient();

        var resposta = await cliente.GetAsync(rota);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }
}
