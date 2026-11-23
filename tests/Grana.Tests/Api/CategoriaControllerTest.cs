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

public sealed class CategoriaControllerTest
{
    private static readonly JsonSerializerOptions _opcoesJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly CategoriaRepository _categoriaRepositorio = new();
    private readonly LancamentoRepository _lancamentoRepositorio = new();
    private readonly CategoriaController _controller;

    public CategoriaControllerTest()
    {
        _controller = new CategoriaController(new CategoriaService(_categoriaRepositorio, _lancamentoRepositorio));
    }

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
    public async Task ListarCategoriasSemFiltro()
    {
        await AdicionarCategoriasDeDespesaEReceita();

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarCategorias(null, CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(3, dados.GetArrayLength());
        Assert.Equal("Alimentação", dados[0].GetProperty("nome").GetString());
        Assert.Equal("Transporte", dados[2].GetProperty("nome").GetString());
    }

    [Fact]
    public async Task ListarCategoriasDeDespesa()
    {
        await AdicionarCategoriasDeDespesaEReceita();

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarCategorias("Despesa", CancellationToken.None));

        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(2, LerJson(resultado).GetProperty("dados").GetArrayLength());
    }

    [Theory]
    [InlineData("Xpto")]
    [InlineData("99")]
    public async Task ListarCategoriasComTipoInvalido(string tipo)
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarCategorias(tipo, CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
    }

    [Fact]
    public async Task ObterCategoriaExistente()
    {
        var criada = Assert.IsType<CreatedAtActionResult>(await _controller.CriarCategoria(new CategoriaCriarDto("Alimentação", "Despesa", 800), CancellationToken.None));
        var id = (Guid)criada.RouteValues["id"];

        var resultado = Assert.IsType<ObjectResult>(await _controller.ObterCategoriaPorId(id, CancellationToken.None));

        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal("Alimentação", LerJson(resultado).GetProperty("dados").GetProperty("nome").GetString());
    }

    [Fact]
    public async Task ObterCategoriaInexistente()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ObterCategoriaPorId(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
    }

    [Fact]
    public async Task EditarCategoriaComDadosValidos()
    {
        var id = await CriarCategoria("Alimentação", "Despesa", 800);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarCategoria(id, new CategoriaEditarDto("Mercado", 1000), CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal("Mercado", dados.GetProperty("nome").GetString());
        Assert.Equal(1000m, dados.GetProperty("orcamentoMensal").GetDecimal());
    }

    [Fact]
    public async Task EditarCategoriaDespesaRemovendoOrcamento()
    {
        var id = await CriarCategoria("Alimentação", "Despesa", 800);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarCategoria(id, new CategoriaEditarDto("Alimentação", null), CancellationToken.None));

        Assert.Equal(200, resultado.StatusCode);
        Assert.False(LerJson(resultado).GetProperty("dados").TryGetProperty("orcamentoMensal", out _));
    }

    [Fact]
    public async Task EditarCategoriaReceitaComOrcamento()
    {
        var id = await CriarCategoria("Salário", "Receita", null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarCategoria(id, new CategoriaEditarDto("Salário", 500), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
    }

    [Fact]
    public async Task EditarCategoriaComNomeInvalido()
    {
        var id = await CriarCategoria("Alimentação", "Despesa", null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarCategoria(id, new CategoriaEditarDto("a", null), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
    }

    [Fact]
    public async Task EditarCategoriaComNomeDeOutraDoMesmoTipo()
    {
        await CriarCategoria("Alimentação", "Despesa", null);
        var id = await CriarCategoria("Transporte", "Despesa", null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarCategoria(id, new CategoriaEditarDto("alimentação", null), CancellationToken.None));

        Assert.Equal(409, resultado.StatusCode);
    }

    [Fact]
    public async Task EditarCategoriaComNomeDeOutraDeOutroTipo()
    {
        await CriarCategoria("Alimentação", "Despesa", null);
        var id = await CriarCategoria("Salário", "Receita", null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarCategoria(id, new CategoriaEditarDto("alimentação", null), CancellationToken.None));

        Assert.Equal(200, resultado.StatusCode);
    }

    [Fact]
    public async Task EditarCategoriaComMesmoNome()
    {
        var id = await CriarCategoria("Alimentação", "Despesa", null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarCategoria(id, new CategoriaEditarDto("ALIMENTAÇÃO", null), CancellationToken.None));

        Assert.Equal(200, resultado.StatusCode);
    }

    [Fact]
    public async Task EditarCategoriaInexistente()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarCategoria(Guid.NewGuid(), new CategoriaEditarDto("Mercado", null), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
    }

    [Fact]
    public async Task ExcluirCategoriaSemLancamentos()
    {
        var id = await CriarCategoria("Alimentação", "Despesa", null);

        var resultado = Assert.IsType<NoContentResult>(await _controller.ExcluirCategoria(id, CancellationToken.None));

        Assert.Equal(204, resultado.StatusCode);
        Assert.Null(await _categoriaRepositorio.ObterCategoriaPorId(id, CancellationToken.None));
    }

    [Fact]
    public async Task ExcluirCategoriaComLancamentos()
    {
        var id = await CriarCategoria("Alimentação", "Despesa", null);
        var categoria = await _categoriaRepositorio.ObterCategoriaPorId(id, CancellationToken.None);
        var lancamento = Lancamento.CriacaoLancamento(Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 100m, new Notificacao()), categoria, 50, DateOnly.FromDateTime(DateTime.UtcNow), null, new Notificacao());
        await _lancamentoRepositorio.AdicionarLancamento(lancamento, CancellationToken.None);

        var resultado = Assert.IsType<ObjectResult>(await _controller.ExcluirCategoria(id, CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("Não é possível excluir uma categoria com lançamentos.", LerJson(resultado).GetProperty("erros")[0].GetString());
        Assert.NotNull(await _categoriaRepositorio.ObterCategoriaPorId(id, CancellationToken.None));
    }

    [Fact]
    public async Task ExcluirCategoriaInexistente()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ExcluirCategoria(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
    }

    private async Task<Guid> CriarCategoria(string nome, string tipo, decimal? orcamentoMensal)
    {
        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarCategoria(new CategoriaCriarDto(nome, tipo, orcamentoMensal), CancellationToken.None));

        return (Guid)resultado.RouteValues["id"];
    }

    private async Task AdicionarCategoriasDeDespesaEReceita()
    {
        await _controller.CriarCategoria(new CategoriaCriarDto("Transporte", "Despesa", null), CancellationToken.None);
        await _controller.CriarCategoria(new CategoriaCriarDto("Salário", "Receita", null), CancellationToken.None);
        await _controller.CriarCategoria(new CategoriaCriarDto("Alimentação", "Despesa", 800), CancellationToken.None);
    }
}
