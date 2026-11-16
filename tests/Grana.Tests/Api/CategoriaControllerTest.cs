using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Controllers;
using Application.Dtos;
using Application.Services;
using Infraestructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Api;

public sealed class CategoriaControllerTest
{
    private static readonly JsonSerializerOptions _opcoesJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly CategoriaController _controller = new(new CategoriaService(new CategoriaRepository()));

    private static JsonElement LerJson(ObjectResult resultado)
        => JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value, _opcoesJson)).RootElement;

    [Fact]
    public async Task CriarCategoriaDespesaComOrcamento()
    {
        var dto = new CategoriaCriarDto("Alimentação", "Despesa", 800);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarCategoria(dto, CancellationToken.None));

        Assert.Equal(201, resultado.StatusCode);
        Assert.Equal(nameof(CategoriaController.ObterCategoriaPorId), resultado.ActionName);
        Assert.True(resultado.RouteValues.ContainsKey("id"));
        Assert.Equal(800m, LerJson(resultado).GetProperty("dados").GetProperty("orcamentoMensal").GetDecimal());
    }

    [Fact]
    public async Task CriarCategoriaReceitaComOrcamento()
    {
        var dto = new CategoriaCriarDto("Salário", "Receita", 500);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarCategoria(dto, CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
    }

    [Fact]
    public async Task CriarCategoriaComNomeDuplicadoNoMesmoTipo()
    {
        await _controller.CriarCategoria(new CategoriaCriarDto("Alimentação", "Despesa", null), CancellationToken.None);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarCategoria(new CategoriaCriarDto("alimentação", "Despesa", null), CancellationToken.None));

        Assert.Equal(409, resultado.StatusCode);
    }

    [Fact]
    public async Task CriarCategoriaComNomeDuplicadoEmOutroTipo()
    {
        await _controller.CriarCategoria(new CategoriaCriarDto("Alimentação", "Despesa", null), CancellationToken.None);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarCategoria(new CategoriaCriarDto("alimentação", "Receita", null), CancellationToken.None));

        Assert.Equal(201, resultado.StatusCode);
    }

    [Fact]
    public async Task CriarCategoriaComTipoInvalido()
    {
        var dto = new CategoriaCriarDto("Alimentação", "Investimento", null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarCategoria(dto, CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
    }

    [Fact]
    public async Task ObterCategoriaInexistente()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ObterCategoriaPorId(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
    }
}
