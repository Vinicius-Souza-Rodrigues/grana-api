using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Controllers;
using Application.Dtos;
using Application.Services;
using Infraestructure.Data;
using Infraestructure.Repositories;
using Infraestructure.Seguranca;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tests.Shared;

namespace Tests.Api;

public sealed class AuthControllerTest
{
    private static readonly JsonSerializerOptions _opcoesJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly GranaDbContext _contexto = BancoEmMemoria.CriarContexto();
    private readonly HashSenha _hashSenha = new();
    private readonly AuthController _controller;

    public AuthControllerTest()
    {
        _controller = new AuthController(new AuthService(new UsuarioRepository(_contexto), _hashSenha, new UnitOfWork(_contexto)));
    }

    private static JsonElement LerJson(ObjectResult resultado)
        => JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value, _opcoesJson)).RootElement;

    [Fact]
    public async Task RegistrarComDadosValidos()
    {
        var dto = new UsuarioRegistrarDto("Maria Silva", "Maria@Email.com", "senhaForte123");

        var resultado = Assert.IsType<ObjectResult>(await _controller.Registrar(dto, CancellationToken.None));
        var dados = LerJson(resultado).GetProperty("dados");

        Assert.Equal(201, resultado.StatusCode);
        Assert.NotEqual(Guid.Empty, dados.GetProperty("id").GetGuid());
        Assert.Equal("Maria Silva", dados.GetProperty("nome").GetString());
        Assert.Equal("maria@email.com", dados.GetProperty("email").GetString());
        Assert.True(dados.TryGetProperty("criadoEm", out _));
    }

    [Fact]
    public async Task RespostaNaoTrazASenhaNemOHash()
    {
        var dto = new UsuarioRegistrarDto("Maria Silva", "maria@email.com", "senhaForte123");

        var resultado = Assert.IsType<ObjectResult>(await _controller.Registrar(dto, CancellationToken.None));
        var json = JsonSerializer.Serialize(resultado.Value, _opcoesJson);
        var dados = LerJson(resultado).GetProperty("dados");

        Assert.DoesNotContain("senhaForte123", json);
        Assert.False(dados.TryGetProperty("senha", out _));
        Assert.False(dados.TryGetProperty("senhaHash", out _));
        Assert.Equal(["id", "nome", "email", "criadoEm"], dados.EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public async Task SenhaFicaGuardadaSoComoHash()
    {
        await _controller.Registrar(new UsuarioRegistrarDto("Maria Silva", "maria@email.com", "senhaForte123"), CancellationToken.None);

        var usuario = await _contexto.Usuarios.SingleAsync();

        Assert.NotEqual("senhaForte123", usuario.SenhaHash);
        Assert.DoesNotContain("senhaForte123", usuario.SenhaHash);
        Assert.True(_hashSenha.Verificar(usuario.SenhaHash, "senhaForte123"));
    }

    [Fact]
    public async Task EmailJaCadastradoComMaiusculasDiferentes()
    {
        await _controller.Registrar(new UsuarioRegistrarDto("Maria Silva", "maria@email.com", "senhaForte123"), CancellationToken.None);

        var resultado = Assert.IsType<ObjectResult>(await _controller.Registrar(new UsuarioRegistrarDto("Outra Maria", "MARIA@Email.com", "outraSenha456"), CancellationToken.None));
        var json = LerJson(resultado);

        Assert.Equal(409, resultado.StatusCode);
        Assert.Equal("Já existe um usuário com esse e-mail.", json.GetProperty("erros")[0].GetString());
        Assert.Equal(1, await _contexto.Usuarios.CountAsync());
    }

    [Fact]
    public async Task NomeVazioESenha123GeramOsDoisErros()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.Registrar(new UsuarioRegistrarDto("", "maria@email.com", "123"), CancellationToken.None));
        var erros = LerJson(resultado).GetProperty("erros");

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal(2, erros.GetArrayLength());
        Assert.Equal(0, await _contexto.Usuarios.CountAsync());
    }

    [Fact]
    public async Task DadosInvalidosNaoChecamEmailDuplicado()
    {
        await _controller.Registrar(new UsuarioRegistrarDto("Maria Silva", "maria@email.com", "senhaForte123"), CancellationToken.None);

        var resultado = Assert.IsType<ObjectResult>(await _controller.Registrar(new UsuarioRegistrarDto("", "maria@email.com", "123"), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
    }
}
