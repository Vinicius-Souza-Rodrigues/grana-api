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

public sealed class ResumoControllerTest
{
    private static readonly JsonSerializerOptions _opcoesJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly LancamentoRepository _lancamentoRepositorio = new();
    private readonly ContaRepository _contaRepositorio = new();
    private readonly ResumoController _controller;

    public ResumoControllerTest()
    {
        _controller = new ResumoController(new ResumoService(_lancamentoRepositorio, _contaRepositorio));
    }

    private static JsonElement LerJson(ObjectResult resultado)
        => JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value, _opcoesJson)).RootElement;

    private async Task<JsonElement> ObterResumo(int? ano, int? mes)
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ObterResumoMensal(new ResumoMensalDto(ano, mes), CancellationToken.None));

        Assert.Equal(200, resultado.StatusCode);

        return LerJson(resultado).GetProperty("dados");
    }

    [Fact]
    public async Task ObterResumoComResultadoDoMes()
    {
        var conta = await CriarConta("Nubank", 1000m);
        await AdicionarLancamento(conta, CriarCategoria("Salário", TipoCategoriaEnum.Receita), 5200m, new DateOnly(2025, 11, 5));
        await AdicionarLancamento(conta, CriarCategoria("Mercado", TipoCategoriaEnum.Despesa), 3480.50m, new DateOnly(2025, 11, 10));

        var dados = await ObterResumo(2025, 11);

        Assert.Equal(2025, dados.GetProperty("ano").GetInt32());
        Assert.Equal(11, dados.GetProperty("mes").GetInt32());
        Assert.Equal(5200m, dados.GetProperty("totalReceitas").GetDecimal());
        Assert.Equal(3480.50m, dados.GetProperty("totalDespesas").GetDecimal());
        Assert.Equal(1719.50m, dados.GetProperty("resultadoDoMes").GetDecimal());
    }

    [Fact]
    public async Task ObterResumoDeMesSemLancamentos()
    {
        var dados = await ObterResumo(2025, 11);

        Assert.Equal(0m, dados.GetProperty("totalReceitas").GetDecimal());
        Assert.Equal(0m, dados.GetProperty("totalDespesas").GetDecimal());
        Assert.Equal(0m, dados.GetProperty("resultadoDoMes").GetDecimal());
        Assert.Equal(0m, dados.GetProperty("saldoAtualContas").GetDecimal());
        Assert.Equal(0, dados.GetProperty("despesasPorCategoria").GetArrayLength());
        Assert.Equal(0, dados.GetProperty("receitasPorCategoria").GetArrayLength());
    }

    [Fact]
    public async Task ObterResumoComMesAnteriorSemDespesas()
    {
        var conta = await CriarConta("Nubank", 1000m);
        var salario = CriarCategoria("Salário", TipoCategoriaEnum.Receita);
        await AdicionarLancamento(conta, salario, 4000m, new DateOnly(2025, 10, 5));
        await AdicionarLancamento(conta, salario, 5000m, new DateOnly(2025, 11, 5));
        await AdicionarLancamento(conta, CriarCategoria("Mercado", TipoCategoriaEnum.Despesa), 900m, new DateOnly(2025, 11, 10));

        var dados = await ObterResumo(2025, 11);

        var comparativo = dados.GetProperty("comparativoMesAnterior");
        Assert.Equal(25.00m, comparativo.GetProperty("receitas").GetDecimal());
        Assert.Equal(JsonValueKind.Null, comparativo.GetProperty("despesas").ValueKind);
    }

    [Fact]
    public async Task ObterResumoComSaldoDasContasAtivas()
    {
        await CriarConta("Nubank", 1000m);
        await CriarConta("Inter", 250.50m);
        var antiga = await CriarConta("Antiga", 0m);
        antiga.Desativar(new Notificacao());

        var dados = await ObterResumo(2025, 11);

        Assert.Equal(1250.50m, dados.GetProperty("saldoAtualContas").GetDecimal());
    }

    [Fact]
    public async Task ObterResumoComTotaisPorCategoria()
    {
        var conta = await CriarConta("Nubank", 1000m);
        var mercado = CriarCategoria("Mercado", TipoCategoriaEnum.Despesa);
        var aluguel = CriarCategoria("Aluguel", TipoCategoriaEnum.Despesa);
        await AdicionarLancamento(conta, mercado, 300m, new DateOnly(2025, 11, 3));
        await AdicionarLancamento(conta, aluguel, 1500m, new DateOnly(2025, 11, 5));
        await AdicionarLancamento(conta, mercado, 200m, new DateOnly(2025, 11, 20));

        var dados = await ObterResumo(2025, 11);

        var despesas = dados.GetProperty("despesasPorCategoria");
        Assert.Equal(2, despesas.GetArrayLength());
        Assert.Equal(aluguel.Id, despesas[0].GetProperty("categoria").GetProperty("id").GetGuid());
        Assert.Equal("Aluguel", despesas[0].GetProperty("categoria").GetProperty("nome").GetString());
        Assert.Equal(1500m, despesas[0].GetProperty("total").GetDecimal());
        Assert.Equal(75.00m, despesas[0].GetProperty("percentual").GetDecimal());
        Assert.Equal("Mercado", despesas[1].GetProperty("categoria").GetProperty("nome").GetString());
        Assert.Equal(500m, despesas[1].GetProperty("total").GetDecimal());
        Assert.Equal(25.00m, despesas[1].GetProperty("percentual").GetDecimal());
    }

    [Fact]
    public async Task ObterResumoSemTransferencias()
    {
        var origem = await CriarConta("Nubank", 1000m);
        var destino = await CriarConta("Inter", 500m);
        var transferencia = Transferencia.CriacaoTransferencia(origem, destino, 300m, DateOnly.FromDateTime(DateTime.UtcNow), null, new Notificacao());
        transferencia.Efetivar(new Notificacao());

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var dados = await ObterResumo(hoje.Year, hoje.Month);

        Assert.Equal(0m, dados.GetProperty("totalReceitas").GetDecimal());
        Assert.Equal(0m, dados.GetProperty("totalDespesas").GetDecimal());
        Assert.Equal(0, dados.GetProperty("receitasPorCategoria").GetArrayLength());
        Assert.Equal(0, dados.GetProperty("despesasPorCategoria").GetArrayLength());
        Assert.Equal(1500m, dados.GetProperty("saldoAtualContas").GetDecimal());
    }

    [Fact]
    public async Task ObterResumoComMesInvalido()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ObterResumoMensal(new ResumoMensalDto(2025, 13), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("O mês deve estar entre 1 e 12.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    private static Categoria CriarCategoria(string nome, TipoCategoriaEnum tipo)
        => Categoria.CriacaoCategoria(nome, tipo, null, new Notificacao());

    private async Task<Conta> CriarConta(string nome, decimal saldo)
    {
        var conta = Conta.CriacaoConta(nome, TipoContaEnum.Corrente, saldo, new Notificacao());
        await _contaRepositorio.AdicionarConta(conta, CancellationToken.None);

        return conta;
    }

    private async Task AdicionarLancamento(Conta conta, Categoria categoria, decimal valor, DateOnly data)
    {
        var lancamento = Lancamento.CriacaoLancamento(conta, categoria, valor, data, null, new Notificacao());
        await _lancamentoRepositorio.AdicionarLancamento(lancamento, CancellationToken.None);
    }
}
