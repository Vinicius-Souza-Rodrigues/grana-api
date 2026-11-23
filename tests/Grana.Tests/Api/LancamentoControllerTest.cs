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

public sealed class LancamentoControllerTest
{
    private static readonly JsonSerializerOptions _opcoesJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly LancamentoRepository _lancamentoRepositorio = new();
    private readonly ContaRepository _contaRepositorio = new();
    private readonly CategoriaRepository _categoriaRepositorio = new();
    private readonly LancamentoController _controller;

    public LancamentoControllerTest()
    {
        _controller = new LancamentoController(
            new LancamentoService(_lancamentoRepositorio, _contaRepositorio, _categoriaRepositorio),
            new ContaService(_contaRepositorio));
    }

    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.UtcNow);

    private static JsonElement LerJson(ObjectResult resultado)
        => JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value, _opcoesJson)).RootElement;

    [Fact]
    public async Task CriarLancamentoDeReceita()
    {
        var conta = await CriarConta(100m);
        var categoria = await CriarCategoria("Salário", TipoCategoriaEnum.Receita, null);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 50m, Hoje, "Freela"), CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        Assert.Equal(201, resultado.StatusCode);
        Assert.Equal(nameof(LancamentoController.ObterLancamentoPorId), resultado.ActionName);
        Assert.Equal(150m, dados.GetProperty("saldoConta").GetDecimal());
        Assert.Equal(50m, dados.GetProperty("lancamento").GetProperty("valor").GetDecimal());
        Assert.Equal(150m, conta.Saldo);
    }

    [Fact]
    public async Task CriarLancamentoDeDespesaZerandoOSaldo()
    {
        var conta = await CriarConta(100m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 100m, Hoje, null), CancellationToken.None));

        Assert.Equal(201, resultado.StatusCode);
        Assert.Equal(0m, LerJson(resultado).GetProperty("dados").GetProperty("saldoConta").GetDecimal());
    }

    [Fact]
    public async Task CriarLancamentoDeDespesaComSaldoInsuficiente()
    {
        var conta = await CriarConta(100m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 100.01m, Hoje, null), CancellationToken.None));

        var erros = LerJson(resultado).GetProperty("erros");
        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal(1, erros.GetArrayLength());
        Assert.Equal("Saldo insuficiente.", erros[0].GetString());
        Assert.Equal(100m, conta.Saldo);
        Assert.False(await _lancamentoRepositorio.ExisteLancamentoComCategoria(categoria.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CriarLancamentoComContaDesativadaValorZeroEDataFutura()
    {
        var conta = await CriarConta(0m);
        conta.Desativar(new Notificacao());
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 0m, Hoje.AddDays(1), null), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal(3, LerJson(resultado).GetProperty("erros").GetArrayLength());
    }

    [Fact]
    public async Task CriarLancamentoQueUltrapassaOOrcamento()
    {
        var conta = await CriarConta(2000m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, 800m);
        await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 700m, Hoje, null), CancellationToken.None);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 150m, Hoje, null), CancellationToken.None));

        var avisos = LerJson(resultado).GetProperty("avisos");
        Assert.Equal(201, resultado.StatusCode);
        Assert.Equal(1, avisos.GetArrayLength());
        Assert.Equal("O orçamento da categoria Mercado foi ultrapassado.", avisos[0].GetString());
        Assert.Equal(1150m, conta.Saldo);
    }

    [Fact]
    public async Task CriarLancamentoQuePassaDe80PorcentoDoOrcamento()
    {
        var conta = await CriarConta(2000m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, 800m);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 700m, Hoje, null), CancellationToken.None));

        Assert.Equal("A categoria Mercado passou de 80% do orçamento.", LerJson(resultado).GetProperty("avisos")[0].GetString());
    }

    [Fact]
    public async Task CriarLancamentoSemAvisoQuandoAbaixoDe80Porcento()
    {
        var conta = await CriarConta(2000m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, 800m);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 100m, Hoje, null), CancellationToken.None));

        Assert.False(LerJson(resultado).TryGetProperty("avisos", out _));
    }

    [Fact]
    public async Task CriarLancamentoComContaInexistente()
    {
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarLancamento(new LancamentoCriarDto(Guid.NewGuid(), categoria.Id, 10m, Hoje, null), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
        Assert.Equal("Conta não encontrada.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    [Fact]
    public async Task CriarLancamentoComCategoriaInexistente()
    {
        var conta = await CriarConta(100m);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, Guid.NewGuid(), 10m, Hoje, null), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
        Assert.Equal("Categoria não encontrada.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    [Fact]
    public async Task ObterLancamentoExistente()
    {
        var idConta = Guid.NewGuid();
        var categoria = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, null, new Notificacao());
        var lancamento = Lancamento.CriacaoLancamento(idConta, categoria, 150.50m, Hoje, "Compra do mês", new Notificacao());
        await _lancamentoRepositorio.AdicionarLancamento(lancamento, CancellationToken.None);

        var resultado = Assert.IsType<ObjectResult>(await _controller.ObterLancamentoPorId(lancamento.Id, CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(lancamento.Id, dados.GetProperty("id").GetGuid());
        Assert.Equal(idConta, dados.GetProperty("contaId").GetGuid());
        Assert.Equal(categoria.Id, dados.GetProperty("categoriaId").GetGuid());
        Assert.Equal(150.50m, dados.GetProperty("valor").GetDecimal());
        Assert.Equal("Compra do mês", dados.GetProperty("descricao").GetString());
    }

    [Fact]
    public async Task ObterLancamentoInexistente()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ObterLancamentoPorId(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
        Assert.Equal("Lançamento não encontrado.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    private async Task<Conta> CriarConta(decimal saldo)
    {
        var conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, saldo, new Notificacao());
        await _contaRepositorio.AdicionarConta(conta, CancellationToken.None);

        return conta;
    }

    private async Task<Categoria> CriarCategoria(string nome, TipoCategoriaEnum tipo, decimal? orcamentoMensal)
    {
        var categoria = Categoria.CriacaoCategoria(nome, tipo, orcamentoMensal, new Notificacao());
        await _categoriaRepositorio.AdicionarCategoria(categoria, CancellationToken.None);

        return categoria;
    }
}
