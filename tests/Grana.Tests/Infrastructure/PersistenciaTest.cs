using Application.Dtos;
using Application.Services;
using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;
using Infraestructure.Data;
using Infraestructure.Repositories;
using Tests.Shared;

namespace Tests.Infrastructure;

public sealed class PersistenciaTest
{
    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly string _nomeBanco = Guid.NewGuid().ToString();
    private readonly Notificacao _notificacao = new();

    private GranaDbContext NovoContexto() => BancoEmMemoria.CriarContexto(_nomeBanco);

    [Fact]
    public async Task LancamentoSalvoCarregaContaECategoriaEmOutroContexto()
    {
        var conta = await SalvarConta("Nubank", 1000m);
        var categoria = await SalvarCategoria("Mercado", TipoCategoriaEnum.Despesa);

        using (var contexto = NovoContexto())
        {
            var service = new LancamentoService(new LancamentoRepository(contexto), new ContaRepository(contexto), new CategoriaRepository(contexto), new UnitOfWork(contexto));
            await service.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 250m, Hoje, "Feira"), _notificacao, CancellationToken.None);
        }

        using var leitura = NovoContexto();
        var salvo = Assert.Single(leitura.Lancamentos);
        var lancamento = await new LancamentoRepository(leitura).ObterLancamentoPorId(salvo.Id, CancellationToken.None);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal("Nubank", lancamento.Conta.Nome);
        Assert.Equal("Mercado", lancamento.Categoria.Nome);
        Assert.Equal(750m, lancamento.Conta.Saldo);
    }

    [Fact]
    public async Task EdicaoDeLancamentoSalvaNovoValorESaldoEmOutroContexto()
    {
        var conta = await SalvarConta("Nubank", 1000m);
        var categoria = await SalvarCategoria("Mercado", TipoCategoriaEnum.Despesa);
        Guid idLancamento;

        using (var contexto = NovoContexto())
        {
            var service = new LancamentoService(new LancamentoRepository(contexto), new ContaRepository(contexto), new CategoriaRepository(contexto), new UnitOfWork(contexto));
            var lancamento = await service.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 250m, Hoje, null), _notificacao, CancellationToken.None);
            idLancamento = lancamento.Id;
        }

        using (var contexto = NovoContexto())
        {
            var service = new LancamentoService(new LancamentoRepository(contexto), new ContaRepository(contexto), new CategoriaRepository(contexto), new UnitOfWork(contexto));
            await service.EditarLancamento(idLancamento, new LancamentoEditarDto(categoria.Id, 400m, Hoje, "Feira do mês"), _notificacao, CancellationToken.None);
        }

        using var leitura = NovoContexto();
        var editado = await new LancamentoRepository(leitura).ObterLancamentoPorId(idLancamento, CancellationToken.None);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(400m, editado.Valor);
        Assert.Equal("Feira do mês", editado.Descricao);
        Assert.Equal(600m, editado.Conta.Saldo);
    }

    [Fact]
    public async Task TransferenciaSalvaCarregaAsDuasContasEmOutroContexto()
    {
        var origem = await SalvarConta("Nubank", 1000m);
        var destino = await SalvarConta("Inter", 0m);

        using (var contexto = NovoContexto())
        {
            var service = new TransferenciaService(new TransferenciaRepository(contexto), new ContaRepository(contexto), new UnitOfWork(contexto));
            await service.CriarTransferencia(new TransferenciaCriarDto(origem.Id, destino.Id, 300m, Hoje, null), _notificacao, CancellationToken.None);
        }

        using var leitura = NovoContexto();
        var transferencia = Assert.Single(await new TransferenciaRepository(leitura).ListarTransferencias(FiltroTransferencia.CriarFiltro(null, null, null, new Notificacao()), CancellationToken.None));

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(700m, transferencia.ContaOrigem.Saldo);
        Assert.Equal(300m, transferencia.ContaDestino.Saldo);
    }

    [Fact]
    public async Task TransferenciaComErroNaoSalvaNada()
    {
        var origem = await SalvarConta("Nubank", 100m);
        var destino = await SalvarConta("Inter", 0m);

        using (var contexto = NovoContexto())
        {
            var service = new TransferenciaService(new TransferenciaRepository(contexto), new ContaRepository(contexto), new UnitOfWork(contexto));
            await service.CriarTransferencia(new TransferenciaCriarDto(origem.Id, destino.Id, 300m, Hoje, null), _notificacao, CancellationToken.None);
        }

        using var leitura = NovoContexto();

        Assert.True(_notificacao.TemNotificacoes);
        Assert.Empty(leitura.Transferencias);
        Assert.Equal(100m, (await new ContaRepository(leitura).ObterContaPorId(origem.Id, CancellationToken.None)).Saldo);
        Assert.Equal(0m, (await new ContaRepository(leitura).ObterContaPorId(destino.Id, CancellationToken.None)).Saldo);
    }

    [Fact]
    public async Task LancamentosDoMesCarregamACategoriaEmOutroContexto()
    {
        var conta = await SalvarConta("Nubank", 1000m);
        var categoria = await SalvarCategoria("Salário", TipoCategoriaEnum.Receita);

        using (var contexto = NovoContexto())
        {
            var service = new LancamentoService(new LancamentoRepository(contexto), new ContaRepository(contexto), new CategoriaRepository(contexto), new UnitOfWork(contexto));
            await service.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 5000m, Hoje, null), _notificacao, CancellationToken.None);
        }

        using var leitura = NovoContexto();
        var lancamento = Assert.Single(await new LancamentoRepository(leitura).ListarLancamentosDoMes(Hoje.Year, Hoje.Month, CancellationToken.None));

        Assert.Equal("Salário", lancamento.Categoria.Nome);
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
