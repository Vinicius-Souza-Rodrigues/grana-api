using System.Net;
using System.Net.Http.Json;
using Application.Interfaces;
using Infraestructure.Data;
using IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests;

public sealed class RegistroUsuarioTest(GranaApiFactory factory) : TesteDeIntegracao(factory)
{
    private const string Senha = "senhaForte123";

    [Fact]
    public async Task RegistrarGuardaSoOHashEASenhaConfere()
    {
        var resposta = await Cliente.PostAsJsonAsync("api/auth/registrar", new { nome = "Maria Silva", email = "Maria@Email.com", senha = Senha });
        var corpo = await resposta.Content.ReadAsStringAsync();
        var dados = await LerDados(resposta);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        Assert.DoesNotContain(Senha, corpo);
        Assert.Equal("maria@email.com", dados.GetProperty("email").GetString());

        using var escopo = Factory.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<GranaDbContext>();
        var hashSenha = escopo.ServiceProvider.GetRequiredService<IHashSenha>();

        var senhaNoBanco = await contexto.Database
            .SqlQuery<string>($"SELECT senha_hash AS \"Value\" FROM usuarios WHERE id = {dados.GetProperty("id").GetGuid()}")
            .SingleAsync();

        Assert.DoesNotContain(Senha, senhaNoBanco);
        Assert.True(hashSenha.Verificar(senhaNoBanco, Senha));
    }

    [Fact]
    public async Task EmailRepetidoComMaiusculasDiferentesRecebeConflito()
    {
        await Cliente.PostAsJsonAsync("api/auth/registrar", new { nome = "Maria Silva", email = "maria@email.com", senha = Senha });

        var resposta = await Cliente.PostAsJsonAsync("api/auth/registrar", new { nome = "Maria Souza", email = "MARIA@EMAIL.COM", senha = Senha });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task NomeVazioESenha123RecebemOsDoisErros()
    {
        var resposta = await Cliente.PostAsJsonAsync("api/auth/registrar", new { nome = "", email = "maria@email.com", senha = "123" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(2, (await LerJson(resposta)).GetProperty("erros").GetArrayLength());
    }
}
