using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Tests.Domain;

public sealed class ResumoMensalTest
{
    private readonly Notificacao _notificacao = new();
    private readonly Conta _conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 100m, new Notificacao());
    private readonly Categoria _salario = Categoria.CriacaoCategoria("Salário", TipoCategoriaEnum.Receita, null, new Notificacao());
    private readonly Categoria _freela = Categoria.CriacaoCategoria("Freela", TipoCategoriaEnum.Receita, null, new Notificacao());
    private readonly Categoria _mercado = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, null, new Notificacao());
    private readonly Categoria _aluguel = Categoria.CriacaoCategoria("Aluguel", TipoCategoriaEnum.Despesa, null, new Notificacao());
    private readonly Categoria _lazer = Categoria.CriacaoCategoria("Lazer", TipoCategoriaEnum.Despesa, null, new Notificacao());

    private Lancamento CriarLancamento(Categoria categoria, decimal valor)
        => Lancamento.CriacaoLancamento(_conta, categoria, valor, new DateOnly(2025, 11, 10), null, new Notificacao());

    private static ResumoMensal CriarResumo(int ano = 2025, int mes = 11)
        => ResumoMensal.CriarResumoMensal(ano, mes, new Notificacao());

    [Fact]
    public void CriarResumoMensalSemParametrosUsaOMesAtual()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        var resumo = ResumoMensal.CriarResumoMensal(null, null, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(hoje.Year, resumo.Ano);
        Assert.Equal(hoje.Month, resumo.Mes);
    }

    [Fact]
    public void CriarResumoMensalComMesInvalido()
    {
        var resumo = ResumoMensal.CriarResumoMensal(2025, 13, _notificacao);

        Assert.Null(resumo);
        Assert.Equal(["O mês deve estar entre 1 e 12."], _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarResumoMensalComAnoInvalido()
    {
        var resumo = ResumoMensal.CriarResumoMensal(2101, 1, _notificacao);

        Assert.Null(resumo);
        Assert.Equal(["O ano deve estar entre 2000 e 2100."], _notificacao.Notificacoes);
    }

    [Fact]
    public void MesAnteriorDeJaneiroEDezembroDoAnoAnterior()
    {
        var resumo = CriarResumo(2025, 1);

        Assert.Equal(2024, resumo.AnoAnterior);
        Assert.Equal(12, resumo.MesAnterior);
    }

    [Fact]
    public void ConsolidarCalculaTotaisEResultadoDoMes()
    {
        var resumo = CriarResumo();

        resumo.Consolidar([CriarLancamento(_salario, 5200m), CriarLancamento(_mercado, 3480.50m)], [], []);

        Assert.Equal(5200m, resumo.TotalReceitas);
        Assert.Equal(3480.50m, resumo.TotalDespesas);
        Assert.Equal(1719.50m, resumo.ResultadoDoMes);
    }

    [Fact]
    public void ConsolidarComResultadoNegativo()
    {
        var resumo = CriarResumo();

        resumo.Consolidar([CriarLancamento(_salario, 1000m), CriarLancamento(_mercado, 1500m)], [], []);

        Assert.Equal(-500m, resumo.ResultadoDoMes);
    }

    [Fact]
    public void ConsolidarSemLancamentosDaZerosEListasVazias()
    {
        var resumo = CriarResumo();

        resumo.Consolidar([], [], []);

        Assert.Equal(0m, resumo.TotalReceitas);
        Assert.Equal(0m, resumo.TotalDespesas);
        Assert.Equal(0m, resumo.ResultadoDoMes);
        Assert.Equal(0m, resumo.SaldoAtualContas);
        Assert.Null(resumo.ComparativoReceitas);
        Assert.Null(resumo.ComparativoDespesas);
        Assert.Empty(resumo.DespesasPorCategoria);
        Assert.Empty(resumo.ReceitasPorCategoria);
    }

    [Fact]
    public void ConsolidarSomaSoAsContasAtivas()
    {
        var resumo = CriarResumo();
        var inter = Conta.CriacaoConta("Inter", TipoContaEnum.Poupanca, 250m, new Notificacao());
        var desativada = Conta.CriacaoConta("Antiga", TipoContaEnum.Corrente, 0m, new Notificacao());
        desativada.Desativar(new Notificacao());

        resumo.Consolidar([], [], [_conta, inter, desativada]);

        Assert.Equal(350m, resumo.SaldoAtualContas);
    }

    [Fact]
    public void ConsolidarCalculaComparativoComOMesAnterior()
    {
        var resumo = CriarResumo();

        resumo.Consolidar(
            [CriarLancamento(_salario, 5500m), CriarLancamento(_mercado, 900m)],
            [CriarLancamento(_salario, 5000m), CriarLancamento(_mercado, 1200m)],
            []);

        Assert.Equal(10.00m, resumo.ComparativoReceitas);
        Assert.Equal(-25.00m, resumo.ComparativoDespesas);
    }

    [Fact]
    public void ConsolidarComMesAnteriorSemDespesasDaComparativoNulo()
    {
        var resumo = CriarResumo();

        resumo.Consolidar(
            [CriarLancamento(_salario, 5000m), CriarLancamento(_mercado, 900m)],
            [CriarLancamento(_salario, 5000m)],
            []);

        Assert.Equal(0.00m, resumo.ComparativoReceitas);
        Assert.Null(resumo.ComparativoDespesas);
    }

    [Fact]
    public void ConsolidarAgrupaPorCategoriaOrdenadoPorTotal()
    {
        var resumo = CriarResumo();

        resumo.Consolidar(
            [
                CriarLancamento(_mercado, 300m),
                CriarLancamento(_aluguel, 1500m),
                CriarLancamento(_mercado, 200m),
                CriarLancamento(_salario, 4000m),
                CriarLancamento(_freela, 1000m)
            ],
            [],
            []);

        Assert.Equal(["Aluguel", "Mercado"], resumo.DespesasPorCategoria.Select(c => c.Categoria.Nome));
        Assert.Equal([1500m, 500m], resumo.DespesasPorCategoria.Select(c => c.Total));
        Assert.Equal([75.00m, 25.00m], resumo.DespesasPorCategoria.Select(c => c.Percentual));
        Assert.Equal(["Salário", "Freela"], resumo.ReceitasPorCategoria.Select(c => c.Categoria.Nome));
        Assert.Equal([80.00m, 20.00m], resumo.ReceitasPorCategoria.Select(c => c.Percentual));
    }

    [Fact]
    public void ConsolidarAjustaPercentuaisParaSomarCem()
    {
        var resumo = CriarResumo();

        resumo.Consolidar([CriarLancamento(_mercado, 100m), CriarLancamento(_aluguel, 100m), CriarLancamento(_lazer, 100m)], [], []);

        Assert.Equal(100m, resumo.DespesasPorCategoria.Sum(c => c.Percentual));
        Assert.Equal([33.34m, 33.33m, 33.33m], resumo.DespesasPorCategoria.Select(c => c.Percentual));
    }
}
