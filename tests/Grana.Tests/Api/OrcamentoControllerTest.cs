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

public sealed class OrcamentoControllerTest
{
    private static readonly JsonSerializerOptions _opcoesJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CategoriaRepository _categoriaRepositorio = new();
    private readonly LancamentoRepository _lancamentoRepositorio = new();
    private readonly OrcamentoController _controller;
    private readonly Conta _conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 10000m, new Notificacao());

    public OrcamentoControllerTest()
    {
        _controller = new OrcamentoController(new OrcamentoService(_categoriaRepositorio, _lancamentoRepositorio));
    }

    private static JsonElement LerJson(ObjectResult resultado)
        => JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value, _opcoesJson)).RootElement;

    [Fact]
    public async Task ListarOrcamentosComGastoDe80Porcento()
    {
        var mercado = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, 400m);
        await AdicionarLancamento(mercado, 320m, new DateOnly(2025, 11, 10));

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarOrcamentos(new OrcamentoListarDto(2025, 11), CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        var item = dados.GetProperty("categorias")[0];
        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(2025, dados.GetProperty("ano").GetInt32());
        Assert.Equal(11, dados.GetProperty("mes").GetInt32());
        Assert.Equal(mercado.Id, item.GetProperty("categoria").GetProperty("id").GetGuid());
        Assert.Equal("Mercado", item.GetProperty("categoria").GetProperty("nome").GetString());
        Assert.Equal(400m, item.GetProperty("limite").GetDecimal());
        Assert.Equal(320m, item.GetProperty("gasto").GetDecimal());
        Assert.Equal(80m, item.GetProperty("disponivel").GetDecimal());
        Assert.Equal(80.00m, item.GetProperty("percentualUsado").GetDecimal());
        Assert.Equal("Alerta", item.GetProperty("situacao").GetString());
    }

    [Fact]
    public async Task ListarOrcamentosConsideraSoAsDespesasDoMes()
    {
        var mercado = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, 400m);
        await AdicionarLancamento(mercado, 100m, new DateOnly(2025, 10, 31));
        await AdicionarLancamento(mercado, 50m, new DateOnly(2025, 11, 1));
        await AdicionarLancamento(mercado, 30m, new DateOnly(2025, 11, 30));
        await AdicionarLancamento(mercado, 70m, new DateOnly(2025, 12, 1));

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarOrcamentos(new OrcamentoListarDto(2025, 11), CancellationToken.None));

        Assert.Equal(80m, LerJson(resultado).GetProperty("dados").GetProperty("categorias")[0].GetProperty("gasto").GetDecimal());
    }

    [Fact]
    public async Task ListarOrcamentosSemCategoriaSemOrcamento()
    {
        var mercado = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, 400m);
        var farmacia = await CriarCategoria("Farmácia", TipoCategoriaEnum.Despesa, null);
        await CriarCategoria("Salário", TipoCategoriaEnum.Receita, null);
        await AdicionarLancamento(farmacia, 90m, new DateOnly(2025, 11, 10));

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarOrcamentos(new OrcamentoListarDto(2025, 11), CancellationToken.None));

        var categorias = LerJson(resultado).GetProperty("dados").GetProperty("categorias");
        Assert.Equal(1, categorias.GetArrayLength());
        Assert.Equal(mercado.Id, categorias[0].GetProperty("categoria").GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task ListarOrcamentosOrdenadosPorPercentualComTotais()
    {
        var lazer = await CriarCategoria("Lazer", TipoCategoriaEnum.Despesa, 200m);
        var mercado = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, 400m);
        var transporte = await CriarCategoria("Transporte", TipoCategoriaEnum.Despesa, 100m);
        await AdicionarLancamento(lazer, 50m, new DateOnly(2025, 11, 5));
        await AdicionarLancamento(mercado, 320m, new DateOnly(2025, 11, 5));
        await AdicionarLancamento(transporte, 150m, new DateOnly(2025, 11, 5));

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarOrcamentos(new OrcamentoListarDto(2025, 11), CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        var categorias = dados.GetProperty("categorias");
        Assert.Equal("Transporte", categorias[0].GetProperty("categoria").GetProperty("nome").GetString());
        Assert.Equal("Estourado", categorias[0].GetProperty("situacao").GetString());
        Assert.Equal("Mercado", categorias[1].GetProperty("categoria").GetProperty("nome").GetString());
        Assert.Equal("Lazer", categorias[2].GetProperty("categoria").GetProperty("nome").GetString());
        Assert.Equal("Dentro", categorias[2].GetProperty("situacao").GetString());
        Assert.Equal(700m, dados.GetProperty("totalOrcado").GetDecimal());
        Assert.Equal(520m, dados.GetProperty("totalGasto").GetDecimal());
    }

    [Fact]
    public async Task ListarOrcamentosSemParametrosUsaOMesAtual()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var mercado = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, 400m);
        await AdicionarLancamento(mercado, 40m, hoje);

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarOrcamentos(new OrcamentoListarDto(null, null), CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        Assert.Equal(hoje.Year, dados.GetProperty("ano").GetInt32());
        Assert.Equal(hoje.Month, dados.GetProperty("mes").GetInt32());
        Assert.Equal(40m, dados.GetProperty("categorias")[0].GetProperty("gasto").GetDecimal());
    }

    [Fact]
    public async Task ListarOrcamentosComMes13()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarOrcamentos(new OrcamentoListarDto(2025, 13), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("O mês deve estar entre 1 e 12.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    [Fact]
    public async Task ListarOrcamentosComAnoForaDoIntervalo()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarOrcamentos(new OrcamentoListarDto(1999, 11), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("O ano deve estar entre 2000 e 2100.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    private async Task<Categoria> CriarCategoria(string nome, TipoCategoriaEnum tipo, decimal? orcamentoMensal)
    {
        var categoria = Categoria.CriacaoCategoria(nome, tipo, orcamentoMensal, new Notificacao());
        await _categoriaRepositorio.AdicionarCategoria(categoria, CancellationToken.None);

        return categoria;
    }

    private async Task AdicionarLancamento(Categoria categoria, decimal valor, DateOnly data)
    {
        var lancamento = Lancamento.CriacaoLancamento(_conta, categoria, valor, data, null, new Notificacao());
        await _lancamentoRepositorio.AdicionarLancamento(lancamento, CancellationToken.None);
    }
}
