using System.Text.Json;
using Api.Controllers;
using Application.Dtos;
using Application.Services;
using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;
using Infraestructure.Data;
using Infraestructure.Data.Configurations;
using Infraestructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Tests.Shared;

namespace Tests.Infrastructure;

public sealed class ConcorrenciaTest
{
    private const string MensagemConflito = "A conta foi alterada por outra operação. Tente novamente.";

    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly string _nomeBanco = Guid.NewGuid().ToString();

    private GranaDbContext NovoContexto() => BancoEmMemoria.CriarContexto(_nomeBanco);

    private static JsonElement LerJson(ObjectResult resultado)
        => JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value)).RootElement;

    private static LancamentoController CriarLancamentoController(GranaDbContext contexto)
        => new(new LancamentoService(new LancamentoRepository(contexto), new ContaRepository(contexto), new CategoriaRepository(contexto), new UnitOfWork(contexto)));

    [Fact]
    public async Task DoisDebitosSimultaneosUmPassaEOOutroRecebeConflito()
    {
        var conta = await SalvarConta("Nubank", 100m);
        var categoria = await SalvarCategoria("Mercado", TipoCategoriaEnum.Despesa);

        using var primeiro = NovoContexto();
        using var segundo = NovoContexto();
        await new ContaRepository(segundo).ObterContaPorId(conta.Id, CancellationToken.None);

        var resultadoPrimeiro = await CriarLancamentoController(primeiro).CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 80m, Hoje, null), CancellationToken.None);
        var resultadoSegundo = Assert.IsType<ObjectResult>(await CriarLancamentoController(segundo).CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 80m, Hoje, null), CancellationToken.None));

        using var leitura = NovoContexto();
        var contaSalva = await new ContaRepository(leitura).ObterContaPorId(conta.Id, CancellationToken.None);
        var erros = LerJson(resultadoSegundo).GetProperty("erros");

        Assert.Equal(201, Assert.IsType<CreatedAtActionResult>(resultadoPrimeiro).StatusCode);
        Assert.Equal(409, resultadoSegundo.StatusCode);
        Assert.Equal(MensagemConflito, erros[0].GetString());
        Assert.Equal(20m, contaSalva.Saldo);
    }

    [Fact]
    public async Task DuasTransferenciasSimultaneasDaMesmaOrigemUmaPassaEAOutraRecebeConflito()
    {
        var origem = await SalvarConta("Nubank", 100m);
        var destino = await SalvarConta("Inter", 0m);
        var notificacaoPrimeira = new Notificacao();
        var notificacaoSegunda = new Notificacao();

        using var primeiro = NovoContexto();
        using var segundo = NovoContexto();
        await new ContaRepository(segundo).ObterContaPorId(origem.Id, CancellationToken.None);
        await new ContaRepository(segundo).ObterContaPorId(destino.Id, CancellationToken.None);

        var transferenciaPrimeira = await new TransferenciaService(new TransferenciaRepository(primeiro), new ContaRepository(primeiro), new UnitOfWork(primeiro))
            .CriarTransferencia(new TransferenciaCriarDto(origem.Id, destino.Id, 70m, Hoje, null), notificacaoPrimeira, CancellationToken.None);
        var transferenciaSegunda = await new TransferenciaService(new TransferenciaRepository(segundo), new ContaRepository(segundo), new UnitOfWork(segundo))
            .CriarTransferencia(new TransferenciaCriarDto(origem.Id, destino.Id, 70m, Hoje, null), notificacaoSegunda, CancellationToken.None);

        using var leitura = NovoContexto();
        var contaRepositorio = new ContaRepository(leitura);

        Assert.NotNull(transferenciaPrimeira);
        Assert.False(notificacaoPrimeira.TemNotificacoes);
        Assert.Null(transferenciaSegunda);
        Assert.Equal(TipoNotificacaoEnum.Conflito, notificacaoSegunda.Tipo);
        Assert.Equal(MensagemConflito, Assert.Single(notificacaoSegunda.Notificacoes));
        Assert.Equal(30m, (await contaRepositorio.ObterContaPorId(origem.Id, CancellationToken.None)).Saldo);
        Assert.Equal(70m, (await contaRepositorio.ObterContaPorId(destino.Id, CancellationToken.None)).Saldo);
    }

    [Fact]
    public async Task ContaRecebeNovaVersaoACadaAlteracaoSalva()
    {
        var conta = await SalvarConta("Nubank", 100m);

        using var contexto = NovoContexto();
        var contaCarregada = await new ContaRepository(contexto).ObterContaPorId(conta.Id, CancellationToken.None);
        var versaoAnterior = contexto.Entry(contaCarregada).Property<Guid>(ContaConfiguration.Versao).CurrentValue;

        contaCarregada.Debitar(10m, new Notificacao());
        await new UnitOfWork(contexto).Salvar(new Notificacao(), CancellationToken.None);

        var versaoNova = contexto.Entry(contaCarregada).Property<Guid>(ContaConfiguration.Versao).CurrentValue;

        Assert.NotEqual(Guid.Empty, versaoAnterior);
        Assert.NotEqual(versaoAnterior, versaoNova);
    }

    [Fact]
    public async Task DebitosEmSequenciaComContaRecarregadaNaoGeramConflito()
    {
        var conta = await SalvarConta("Nubank", 100m);
        var categoria = await SalvarCategoria("Mercado", TipoCategoriaEnum.Despesa);

        using (var primeiro = NovoContexto())
            await CriarLancamentoController(primeiro).CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 30m, Hoje, null), CancellationToken.None);

        using var segundo = NovoContexto();
        var resultado = await CriarLancamentoController(segundo).CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 30m, Hoje, null), CancellationToken.None);

        using var leitura = NovoContexto();

        Assert.Equal(201, Assert.IsType<CreatedAtActionResult>(resultado).StatusCode);
        Assert.Equal(40m, (await new ContaRepository(leitura).ObterContaPorId(conta.Id, CancellationToken.None)).Saldo);
    }

    private async Task<Conta> SalvarConta(string nome, decimal saldo)
    {
        using var contexto = NovoContexto();
        var conta = Conta.CriacaoConta(nome, TipoContaEnum.Corrente, saldo, new Notificacao());
        await new ContaRepository(contexto).AdicionarConta(conta, CancellationToken.None);
        await contexto.SaveChangesAsync();

        return conta;
    }

    private async Task<Categoria> SalvarCategoria(string nome, TipoCategoriaEnum tipo)
    {
        using var contexto = NovoContexto();
        var categoria = Categoria.CriacaoCategoria(nome, tipo, null, new Notificacao());
        await new CategoriaRepository(contexto).AdicionarCategoria(categoria, CancellationToken.None);
        await contexto.SaveChangesAsync();

        return categoria;
    }
}
