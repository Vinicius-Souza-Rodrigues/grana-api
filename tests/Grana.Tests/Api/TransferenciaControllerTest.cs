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

public sealed class TransferenciaControllerTest
{
    private static readonly JsonSerializerOptions _opcoesJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly TransferenciaRepository _transferenciaRepositorio = new();
    private readonly ContaRepository _contaRepositorio = new();
    private readonly TransferenciaController _controller;
    private readonly Conta _nubank = CriarConta("Nubank");
    private readonly Conta _inter = CriarConta("Inter");
    private readonly Conta _carteira = CriarConta("Carteira");

    public TransferenciaControllerTest()
    {
        _controller = new TransferenciaController(new TransferenciaService(_transferenciaRepositorio, _contaRepositorio));
    }

    [Fact]
    public async Task CriarTransferenciaAtualizaOsDoisSaldos()
    {
        var origem = await AdicionarConta("Origem", 1500m);
        var destino = await AdicionarConta("Destino", 500m);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarTransferencia(new TransferenciaCriarDto(origem.Id, destino.Id, 300m, Hoje, "Reserva"), CancellationToken.None));

        var dados = JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value, _opcoesJson)).RootElement.GetProperty("dados");
        Assert.Equal(201, resultado.StatusCode);
        Assert.Equal(nameof(TransferenciaController.ObterTransferenciaPorId), resultado.ActionName);
        Assert.Equal(1200m, dados.GetProperty("saldoContaOrigem").GetDecimal());
        Assert.Equal(800m, dados.GetProperty("saldoContaDestino").GetDecimal());
        Assert.Equal(origem.Id, dados.GetProperty("transferencia").GetProperty("contaOrigem").GetProperty("id").GetGuid());
        Assert.Equal(destino.Id, dados.GetProperty("transferencia").GetProperty("contaDestino").GetProperty("id").GetGuid());
        Assert.Equal(1200m, origem.Saldo);
        Assert.Equal(800m, destino.Saldo);
    }

    [Fact]
    public async Task CriarTransferenciaGravaATransferencia()
    {
        var origem = await AdicionarConta("Origem", 1500m);
        var destino = await AdicionarConta("Destino", 500m);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarTransferencia(new TransferenciaCriarDto(origem.Id, destino.Id, 300m, Hoje, null), CancellationToken.None));

        var id = JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value, _opcoesJson)).RootElement.GetProperty("dados").GetProperty("transferencia").GetProperty("id").GetGuid();
        var obtida = Assert.IsType<ObjectResult>(await _controller.ObterTransferenciaPorId(id, CancellationToken.None));
        Assert.Equal(200, obtida.StatusCode);
    }

    [Fact]
    public async Task CriarTransferenciaComSaldoInsuficienteEDestinoDesativado()
    {
        var origem = await AdicionarConta("Origem", 100m);
        var destino = await AdicionarConta("Destino", 0m);
        destino.Desativar(new Notificacao());

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarTransferencia(new TransferenciaCriarDto(origem.Id, destino.Id, 300m, Hoje, null), CancellationToken.None));

        var erros = LerJson(resultado).GetProperty("erros").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal(2, erros.Count);
        Assert.Contains("Saldo insuficiente.", erros);
        Assert.Contains("A conta de destino está desativada.", erros);
        Assert.Equal(100m, origem.Saldo);
        Assert.Equal(0m, destino.Saldo);
        Assert.Empty(await _transferenciaRepositorio.ListarTransferencias(FiltroTransferencia.CriarFiltro(null, null, null, new Notificacao()), CancellationToken.None));
    }

    [Fact]
    public async Task CriarTransferenciaComContaDeOrigemInexistente()
    {
        var destino = await AdicionarConta("Destino", 500m);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarTransferencia(new TransferenciaCriarDto(Guid.NewGuid(), destino.Id, 300m, Hoje, null), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
        Assert.Equal("Conta de origem não encontrada.", LerJson(resultado).GetProperty("erros")[0].GetString());
        Assert.Equal(500m, destino.Saldo);
    }

    [Fact]
    public async Task CriarTransferenciaComContaDeDestinoInexistente()
    {
        var origem = await AdicionarConta("Origem", 1500m);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarTransferencia(new TransferenciaCriarDto(origem.Id, Guid.NewGuid(), 300m, Hoje, null), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
        Assert.Equal("Conta de destino não encontrada.", LerJson(resultado).GetProperty("erros")[0].GetString());
        Assert.Equal(1500m, origem.Saldo);
    }

    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.UtcNow);

    private static JsonElement LerJson(ObjectResult resultado)
        => JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value, _opcoesJson)).RootElement;

    [Fact]
    public async Task ListarTransferenciasDaContaComoOrigemOuDestino()
    {
        await AdicionarTransferencia(_nubank, _inter, 10m, Hoje, "Saída do Nubank");
        await AdicionarTransferencia(_inter, _nubank, 20m, Hoje, "Entrada no Nubank");
        await AdicionarTransferencia(_inter, _carteira, 30m, Hoje, "Sem o Nubank");

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarTransferencias(new TransferenciaListarDto(_nubank.Id, null, null), CancellationToken.None));

        var descricoes = LerJson(resultado).GetProperty("dados").EnumerateArray().Select(t => t.GetProperty("descricao").GetString()).ToList();
        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(2, descricoes.Count);
        Assert.Contains("Saída do Nubank", descricoes);
        Assert.Contains("Entrada no Nubank", descricoes);
    }

    [Fact]
    public async Task ListarTransferenciasSemFiltro()
    {
        await AdicionarTransferencia(_nubank, _inter, 10m, Hoje, null);
        await AdicionarTransferencia(_inter, _carteira, 30m, Hoje, null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarTransferencias(new TransferenciaListarDto(null, null, null), CancellationToken.None));

        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(2, LerJson(resultado).GetProperty("dados").GetArrayLength());
    }

    [Fact]
    public async Task ListarTransferenciasOrdenadasPorDataDecrescente()
    {
        await AdicionarTransferencia(_nubank, _inter, 10m, Hoje.AddDays(-2), "Antiga");
        await AdicionarTransferencia(_nubank, _inter, 10m, Hoje, "Recente");
        await AdicionarTransferencia(_nubank, _inter, 10m, Hoje.AddDays(-1), "Meio");

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarTransferencias(new TransferenciaListarDto(null, null, null), CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        Assert.Equal("Recente", dados[0].GetProperty("descricao").GetString());
        Assert.Equal("Meio", dados[1].GetProperty("descricao").GetString());
        Assert.Equal("Antiga", dados[2].GetProperty("descricao").GetString());
    }

    [Fact]
    public async Task ListarTransferenciasPorPeriodo()
    {
        await AdicionarTransferencia(_nubank, _inter, 10m, Hoje.AddDays(-10), "Fora");
        await AdicionarTransferencia(_nubank, _inter, 10m, Hoje.AddDays(-5), "Início");
        await AdicionarTransferencia(_nubank, _inter, 10m, Hoje.AddDays(-3), "Fim");
        await AdicionarTransferencia(_nubank, _inter, 10m, Hoje, "Depois");

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarTransferencias(new TransferenciaListarDto(null, Hoje.AddDays(-5), Hoje.AddDays(-3)), CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        Assert.Equal(2, dados.GetArrayLength());
        Assert.Equal("Fim", dados[0].GetProperty("descricao").GetString());
        Assert.Equal("Início", dados[1].GetProperty("descricao").GetString());
    }

    [Fact]
    public async Task ListarTransferenciasComDataInicioMaiorQueDataFim()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarTransferencias(new TransferenciaListarDto(null, Hoje, Hoje.AddDays(-1)), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("A data de início não pode ser maior que a data de fim.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    [Fact]
    public async Task ObterTransferenciaExistente()
    {
        var transferencia = await AdicionarTransferencia(_nubank, _inter, 40m, Hoje, "Reserva");

        var resultado = Assert.IsType<ObjectResult>(await _controller.ObterTransferenciaPorId(transferencia.Id, CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(transferencia.Id, dados.GetProperty("id").GetGuid());
        Assert.Equal(40m, dados.GetProperty("valor").GetDecimal());
        Assert.Equal("Reserva", dados.GetProperty("descricao").GetString());
        Assert.Equal(_nubank.Id, dados.GetProperty("contaOrigem").GetProperty("id").GetGuid());
        Assert.Equal("Nubank", dados.GetProperty("contaOrigem").GetProperty("nome").GetString());
        Assert.Equal(_inter.Id, dados.GetProperty("contaDestino").GetProperty("id").GetGuid());
        Assert.Equal("Inter", dados.GetProperty("contaDestino").GetProperty("nome").GetString());
    }

    [Fact]
    public async Task ObterTransferenciaInexistente()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ObterTransferenciaPorId(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
        Assert.Equal("Transferência não encontrada.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    private static Conta CriarConta(string nome)
        => Conta.CriacaoConta(nome, TipoContaEnum.Corrente, 1000m, new Notificacao());

    private async Task<Conta> AdicionarConta(string nome, decimal saldo)
    {
        var conta = Conta.CriacaoConta(nome, TipoContaEnum.Corrente, saldo, new Notificacao());
        await _contaRepositorio.AdicionarConta(conta, CancellationToken.None);

        return conta;
    }

    private async Task<Transferencia> AdicionarTransferencia(Conta origem, Conta destino, decimal valor, DateOnly data, string descricao)
    {
        var transferencia = Transferencia.CriacaoTransferencia(origem, destino, valor, data, descricao, new Notificacao());
        await _transferenciaRepositorio.AdicionarTransferencia(transferencia, CancellationToken.None);

        return transferencia;
    }
}
