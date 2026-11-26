using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Controllers;
using Application.Dtos;
using Application.Services;
using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;
using Infraestructure.Data;
using Infraestructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Tests.Shared;

namespace Tests.Api;

public sealed class ExtratoControllerTest
{
    private static readonly JsonSerializerOptions _opcoesJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly GranaDbContext _contexto = BancoEmMemoria.CriarContexto();
    private readonly ContaRepository _contaRepositorio;
    private readonly LancamentoRepository _lancamentoRepositorio;
    private readonly TransferenciaRepository _transferenciaRepositorio;
    private readonly ExtratoController _controller;
    private readonly Categoria _salario = Categoria.CriacaoCategoria("Salário", TipoCategoriaEnum.Receita, null, new Notificacao());
    private readonly Categoria _mercado = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, null, new Notificacao());

    public ExtratoControllerTest()
    {
        _contaRepositorio = new ContaRepository(_contexto);
        _lancamentoRepositorio = new LancamentoRepository(_contexto);
        _transferenciaRepositorio = new TransferenciaRepository(_contexto);
        _controller = new ExtratoController(new ExtratoService(_contaRepositorio, _lancamentoRepositorio, _transferenciaRepositorio));
    }

    private static JsonElement LerJson(ObjectResult resultado)
        => JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value, _opcoesJson)).RootElement;

    private async Task<ObjectResult> ObterExtrato(Guid idConta, DateOnly? dataInicio, DateOnly? dataFim)
        => Assert.IsType<ObjectResult>(await _controller.ObterExtratoConta(idConta, new ExtratoContaDto(dataInicio, dataFim), CancellationToken.None));

    private async Task<JsonElement> ObterDados(Guid idConta, DateOnly? dataInicio, DateOnly? dataFim)
    {
        var resultado = await ObterExtrato(idConta, dataInicio, dataFim);

        Assert.Equal(200, resultado.StatusCode);

        return LerJson(resultado).GetProperty("dados");
    }

    [Fact]
    public async Task ObterExtratoComSaldoAposDoUltimoItemIgualAoSaldoFinal()
    {
        var conta = await CriarConta("Nubank", 1000m);
        var inter = await CriarConta("Inter", 0m);
        await AdicionarLancamento(conta, _salario, 5000m, Hoje.AddDays(-20));
        await AdicionarLancamento(conta, _mercado, 800m, Hoje.AddDays(-10));
        await AdicionarTransferencia(conta, inter, 1000m, Hoje.AddDays(-5));

        var dados = await ObterDados(conta.Id, Hoje.AddDays(-15), Hoje);

        var movimentacoes = dados.GetProperty("movimentacoes");
        Assert.Equal(6000m, dados.GetProperty("saldoAnterior").GetDecimal());
        Assert.Equal(0m, dados.GetProperty("totalEntradas").GetDecimal());
        Assert.Equal(1800m, dados.GetProperty("totalSaidas").GetDecimal());
        Assert.Equal(4200m, dados.GetProperty("saldoFinal").GetDecimal());
        Assert.Equal(2, movimentacoes.GetArrayLength());
        Assert.Equal("Despesa", movimentacoes[0].GetProperty("tipo").GetString());
        Assert.Equal(-800m, movimentacoes[0].GetProperty("valor").GetDecimal());
        Assert.Equal(5200m, movimentacoes[0].GetProperty("saldoApos").GetDecimal());
        Assert.Equal("TransferenciaEnviada", movimentacoes[1].GetProperty("tipo").GetString());
        Assert.Equal(-1000m, movimentacoes[1].GetProperty("valor").GetDecimal());
        Assert.Equal(dados.GetProperty("saldoFinal").GetDecimal(), movimentacoes[1].GetProperty("saldoApos").GetDecimal());
    }

    [Fact]
    public async Task ObterExtratoAteHojeComSaldoFinalIgualAoSaldoAtual()
    {
        var conta = await CriarConta("Nubank", 1000m);
        var inter = await CriarConta("Inter", 300m);
        await AdicionarLancamento(conta, _salario, 2500m, Hoje.AddDays(-40));
        await AdicionarLancamento(conta, _mercado, 450.75m, Hoje.AddDays(-3));
        await AdicionarTransferencia(conta, inter, 700m, Hoje.AddDays(-2));
        await AdicionarTransferencia(inter, conta, 100m, Hoje);

        var dados = await ObterDados(conta.Id, Hoje.AddDays(-30), Hoje);

        Assert.Equal(conta.Saldo, dados.GetProperty("saldoFinal").GetDecimal());
        Assert.Equal(2449.25m, conta.Saldo);
        Assert.Equal("TransferenciaRecebida", dados.GetProperty("movimentacoes")[2].GetProperty("tipo").GetString());
    }

    [Fact]
    public async Task ObterExtratoSemDatasUsaOMesAtual()
    {
        var conta = await CriarConta("Nubank", 100m);

        var dados = await ObterDados(conta.Id, null, null);

        Assert.Equal(new DateOnly(Hoje.Year, Hoje.Month, 1), DateOnly.Parse(dados.GetProperty("dataInicio").GetString()));
        Assert.Equal(conta.Id, dados.GetProperty("conta").GetProperty("id").GetGuid());
        Assert.Equal(100m, dados.GetProperty("saldoFinal").GetDecimal());
    }

    [Fact]
    public async Task ObterExtratoDeContaInativa()
    {
        var conta = await CriarConta("Antiga", 100m);
        await AdicionarLancamento(conta, _mercado, 100m, Hoje.AddDays(-1));
        conta.Desativar(new Notificacao());
        await _contexto.SaveChangesAsync();

        var dados = await ObterDados(conta.Id, Hoje.AddDays(-7), Hoje);

        Assert.Equal(100m, dados.GetProperty("saldoAnterior").GetDecimal());
        Assert.Equal(0m, dados.GetProperty("saldoFinal").GetDecimal());
    }

    [Fact]
    public async Task ObterExtratoComPeriodoDeDoisAnos()
    {
        var conta = await CriarConta("Nubank", 100m);

        var resultado = await ObterExtrato(conta.Id, new DateOnly(2023, 1, 1), new DateOnly(2024, 12, 31));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("O período do extrato não pode ter mais que 366 dias.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    [Fact]
    public async Task ObterExtratoDeContaInexistente()
    {
        var resultado = await ObterExtrato(Guid.NewGuid(), null, null);

        Assert.Equal(404, resultado.StatusCode);
        Assert.Equal("Conta não encontrada.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    private async Task<Conta> CriarConta(string nome, decimal saldo)
    {
        var conta = Conta.CriacaoConta(nome, TipoContaEnum.Corrente, saldo, new Notificacao());
        await _contaRepositorio.AdicionarConta(conta, CancellationToken.None);
        await _contexto.SaveChangesAsync();

        return conta;
    }

    private async Task AdicionarLancamento(Conta conta, Categoria categoria, decimal valor, DateOnly data)
    {
        var lancamento = Lancamento.CriacaoLancamento(conta, categoria, valor, data, null, new Notificacao());
        conta.AplicarLancamento(lancamento, new Notificacao());
        await _lancamentoRepositorio.AdicionarLancamento(lancamento, CancellationToken.None);
        await _contexto.SaveChangesAsync();
    }

    private async Task AdicionarTransferencia(Conta origem, Conta destino, decimal valor, DateOnly data)
    {
        var transferencia = Transferencia.CriacaoTransferencia(origem, destino, valor, data, null, new Notificacao());
        transferencia.Efetivar(new Notificacao());
        await _contexto.SaveChangesAsync();
        await _transferenciaRepositorio.AdicionarTransferencia(transferencia, CancellationToken.None);
        await _contexto.SaveChangesAsync();
    }
}
