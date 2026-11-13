using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Controllers;
using Application.Dtos;
using Application.Services;
using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;
using Infraestructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Api;

public sealed class ContaControllerTest
{
    private static readonly JsonSerializerOptions _opcoesJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ContaRepository _repositorio = new();
    private readonly ContaController _controller;

    public ContaControllerTest()
    {
        _controller = new ContaController(new ContaService(_repositorio));
    }

    private static JsonElement LerJson(ObjectResult resultado)
        => JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value, _opcoesJson)).RootElement;

    [Fact]
    public async Task CriarContaValida()
    {
        var dto = new ContaCriarDto("Nubank", "Corrente", 1500);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarConta(dto, CancellationToken.None));

        Assert.Equal(201, resultado.StatusCode);
        Assert.Equal(nameof(ContaController.ObterContaPorId), resultado.ActionName);
        Assert.True(resultado.RouteValues.ContainsKey("id"));
        Assert.Equal(1500m, LerJson(resultado).GetProperty("dados").GetProperty("saldo").GetDecimal());
    }

    [Fact]
    public async Task CriarContaComTresErros()
    {
        var dto = new ContaCriarDto("ab", "Investimento", -10);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarConta(dto, CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal(3, LerJson(resultado).GetProperty("erros").GetArrayLength());
    }

    [Fact]
    public async Task CriarContaComNomeDuplicado()
    {
        await _controller.CriarConta(new ContaCriarDto("Nubank", "Corrente", 0), CancellationToken.None);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarConta(new ContaCriarDto("nubank", "Corrente", 0), CancellationToken.None));

        Assert.Equal(409, resultado.StatusCode);
    }

    [Fact]
    public async Task CriarContaComSaldoComTresCasasDecimais()
    {
        var dto = new ContaCriarDto("Nubank", "Corrente", 10.555m);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarConta(dto, CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
    }

    [Fact]
    public async Task ListarContasSemInativas()
    {
        await AdicionarDuasAtivasEUmaInativa();

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarContas(false, CancellationToken.None));

        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(2, LerJson(resultado).GetProperty("dados").GetArrayLength());
    }

    [Fact]
    public async Task ListarContasComInativas()
    {
        await AdicionarDuasAtivasEUmaInativa();

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarContas(true, CancellationToken.None));

        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(3, LerJson(resultado).GetProperty("dados").GetArrayLength());
    }

    [Fact]
    public async Task ObterContaInexistente()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ObterContaPorId(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
    }

    [Fact]
    public async Task RenomearContaComNomeValido()
    {
        var conta = await AdicionarConta("Nubank");

        var resultado = Assert.IsType<ObjectResult>(await _controller.RenomearConta(conta.Id, new ContaRenomearDto("Nubank PJ"), CancellationToken.None));

        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal("Nubank PJ", LerJson(resultado).GetProperty("dados").GetProperty("nome").GetString());
    }

    [Fact]
    public async Task RenomearContaComMesmoNome()
    {
        var conta = await AdicionarConta("Nubank");

        var resultado = Assert.IsType<ObjectResult>(await _controller.RenomearConta(conta.Id, new ContaRenomearDto("Nubank"), CancellationToken.None));

        Assert.Equal(200, resultado.StatusCode);
    }

    [Fact]
    public async Task RenomearContaInativa()
    {
        var conta = await AdicionarConta("Nubank");
        conta.Desativar(new Notificacao());

        var resultado = Assert.IsType<ObjectResult>(await _controller.RenomearConta(conta.Id, new ContaRenomearDto("Nubank PJ"), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("Não é possível editar uma conta desativada.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    [Fact]
    public async Task RenomearContaComNomeDeOutraContaAtiva()
    {
        await AdicionarConta("Nubank");
        var conta = await AdicionarConta("Inter");

        var resultado = Assert.IsType<ObjectResult>(await _controller.RenomearConta(conta.Id, new ContaRenomearDto("nubank"), CancellationToken.None));

        Assert.Equal(409, resultado.StatusCode);
    }

    private async Task<Conta> AdicionarConta(string nome)
    {
        var conta = Conta.CriacaoConta(nome, TipoContaEnum.Corrente, 0, new Notificacao());

        await _repositorio.AdicionarConta(conta, CancellationToken.None);

        return conta;
    }

    private async Task AdicionarDuasAtivasEUmaInativa()
    {
        var inativa = Conta.CriacaoConta("Carteira", TipoContaEnum.Carteira, 0, new Notificacao());
        inativa.Desativar(new Notificacao());

        await _repositorio.AdicionarConta(Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 100, new Notificacao()), CancellationToken.None);
        await _repositorio.AdicionarConta(Conta.CriacaoConta("Inter", TipoContaEnum.Poupanca, 50, new Notificacao()), CancellationToken.None);
        await _repositorio.AdicionarConta(inativa, CancellationToken.None);
    }
}
