using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;
using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;
using Tests.Shared;

namespace Tests.Infrastructure;

public sealed class SeedDemoTest
{
    private readonly string _nomeBanco = Guid.NewGuid().ToString();

    private GranaDbContext NovoContexto() => BancoEmMemoria.CriarContexto(_nomeBanco);

    [Theory]
    [InlineData("2026-11-26")]
    [InlineData("2026-11-02")]
    [InlineData("2026-01-15")]
    public async Task SeedCriaContasCategoriasLancamentosETransferencias(string dataHoje)
    {
        var hoje = DateOnly.Parse(dataHoje);

        using (var contexto = NovoContexto())
            await new SeedDemo(contexto).Executar(hoje, CancellationToken.None);

        using var leitura = NovoContexto();

        Assert.Equal(3, await leitura.Contas.CountAsync());
        Assert.Equal(6, await leitura.Categorias.CountAsync());
        Assert.Equal(14, await leitura.Lancamentos.CountAsync());
        Assert.Equal(2, await leitura.Transferencias.CountAsync());
        Assert.True(await leitura.Lancamentos.AllAsync(l => l.Data <= hoje));
        Assert.True(await leitura.Lancamentos.AnyAsync(l => l.Data < new DateOnly(hoje.Year, hoje.Month, 1)));
    }

    [Fact]
    public async Task SaldoDeCadaContaBateComOsLancamentosEAsTransferencias()
    {
        using (var contexto = NovoContexto())
            await new SeedDemo(contexto).Executar(new DateOnly(2026, 11, 26), CancellationToken.None);

        using var leitura = NovoContexto();
        var lancamentos = await leitura.Lancamentos.ToListAsync();
        var transferencias = await leitura.Transferencias.ToListAsync();

        foreach (var conta in await leitura.Contas.ToListAsync())
        {
            var receitas = lancamentos.Where(l => l.IdConta == conta.Id && l.TipoLancamento == TipoCategoriaEnum.Receita).Sum(l => l.Valor);
            var despesas = lancamentos.Where(l => l.IdConta == conta.Id && l.TipoLancamento == TipoCategoriaEnum.Despesa).Sum(l => l.Valor);
            var recebidas = transferencias.Where(t => t.IdContaDestino == conta.Id).Sum(t => t.Valor);
            var enviadas = transferencias.Where(t => t.IdContaOrigem == conta.Id).Sum(t => t.Valor);

            Assert.Equal(conta.SaldoInicial + receitas - despesas + recebidas - enviadas, conta.Saldo);
            Assert.True(conta.Saldo >= 0);
        }
    }

    [Fact]
    public async Task SeedRodandoDuasVezesNaoDuplicaOsDados()
    {
        var hoje = new DateOnly(2026, 11, 26);

        using (var contexto = NovoContexto())
            await new SeedDemo(contexto).Executar(hoje, CancellationToken.None);

        using (var contexto = NovoContexto())
            await new SeedDemo(contexto).Executar(hoje, CancellationToken.None);

        using var leitura = NovoContexto();

        Assert.Equal(3, await leitura.Contas.CountAsync());
        Assert.Equal(14, await leitura.Lancamentos.CountAsync());
    }

    [Fact]
    public async Task SeedNaoRodaQuandoJaExistemContas()
    {
        using (var contexto = NovoContexto())
        {
            contexto.Contas.Add(Conta.CriacaoConta("Minha conta", TipoContaEnum.Corrente, 100m, new Notificacao()));
            await contexto.SaveChangesAsync();
        }

        using (var contexto = NovoContexto())
            await new SeedDemo(contexto).Executar(new DateOnly(2026, 11, 26), CancellationToken.None);

        using var leitura = NovoContexto();

        Assert.Equal(1, await leitura.Contas.CountAsync());
        Assert.Equal(0, await leitura.Categorias.CountAsync());
        Assert.Equal(0, await leitura.Lancamentos.CountAsync());
    }
}
