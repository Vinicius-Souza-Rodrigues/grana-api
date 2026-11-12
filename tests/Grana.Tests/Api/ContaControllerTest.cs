using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Controllers;
using Application.Dtos;
using Application.Services;
using Infraestructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Api;

public sealed class ContaControllerTest
{
    private static readonly JsonSerializerOptions _opcoesJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ContaController _controller = new(new ContaService(new ContaRepository()));

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
        Assert.Equal(1500m, LerJson(resultado).GetProperty("dados").GetProperty("Saldo").GetDecimal());
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
}
