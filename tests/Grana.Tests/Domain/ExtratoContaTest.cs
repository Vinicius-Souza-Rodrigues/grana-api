using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Tests.Domain;

public sealed class ExtratoContaTest
{
    private readonly Notificacao _notificacao = new();
    private readonly Conta _conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 1000m, new Notificacao());
    private readonly Conta _inter = Conta.CriacaoConta("Inter", TipoContaEnum.Corrente, 500m, new Notificacao());
    private readonly Categoria _salario = Categoria.CriacaoCategoria("Salário", TipoCategoriaEnum.Receita, null, new Notificacao());
    private readonly Categoria _mercado = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, null, new Notificacao());

    private Lancamento CriarLancamento(Conta conta, Categoria categoria, decimal valor, DateOnly data)
        => Lancamento.CriacaoLancamento(conta, categoria, valor, data, null, new Notificacao());

    private static Transferencia CriarTransferencia(Conta origem, Conta destino, decimal valor, DateOnly data)
        => Transferencia.CriacaoTransferencia(origem, destino, valor, data, null, new Notificacao());

    private ExtratoConta CriarExtrato(DateOnly dataInicio, DateOnly dataFim)
        => ExtratoConta.CriarExtratoConta(_conta, dataInicio, dataFim, new Notificacao());

    [Fact]
    public void CriarExtratoSemDatasUsaOMesAtual()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        var extrato = ExtratoConta.CriarExtratoConta(_conta, null, null, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(new DateOnly(hoje.Year, hoje.Month, 1), extrato.DataInicio);
        Assert.Equal(new DateOnly(hoje.Year, hoje.Month, DateTime.DaysInMonth(hoje.Year, hoje.Month)), extrato.DataFim);
    }

    [Fact]
    public void CriarExtratoComPeriodoDeDoisAnos()
    {
        var extrato = ExtratoConta.CriarExtratoConta(_conta, new DateOnly(2023, 1, 1), new DateOnly(2024, 12, 31), _notificacao);

        Assert.Null(extrato);
        Assert.Equal(["O período do extrato não pode ter mais que 366 dias."], _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarExtratoComPeriodoDe366Dias()
    {
        var extrato = ExtratoConta.CriarExtratoConta(_conta, new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31), _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.NotNull(extrato);
    }

    [Fact]
    public void CriarExtratoComDataInicioMaiorQueDataFim()
    {
        var extrato = ExtratoConta.CriarExtratoConta(_conta, new DateOnly(2025, 11, 30), new DateOnly(2025, 11, 1), _notificacao);

        Assert.Null(extrato);
        Assert.Equal(["A data de início não pode ser maior que a data de fim."], _notificacao.Notificacoes);
    }

    [Fact]
    public void ConsolidarCalculaSaldoAnteriorComMovimentacoesAntesDoPeriodo()
    {
        var extrato = CriarExtrato(new DateOnly(2025, 11, 1), new DateOnly(2025, 11, 30));

        extrato.Consolidar(
            [CriarLancamento(_conta, _salario, 300m, new DateOnly(2025, 10, 5)), CriarLancamento(_conta, _mercado, 100m, new DateOnly(2025, 10, 20))],
            [CriarTransferencia(_conta, _inter, 50m, new DateOnly(2025, 10, 25))]);

        Assert.Equal(1150m, extrato.SaldoAnterior);
        Assert.Empty(extrato.Movimentacoes);
        Assert.Equal(1150m, extrato.SaldoFinal);
    }

    [Fact]
    public void ConsolidarIdentificaOsQuatroTipos()
    {
        var extrato = CriarExtrato(new DateOnly(2025, 11, 1), new DateOnly(2025, 11, 30));

        extrato.Consolidar(
            [CriarLancamento(_conta, _salario, 500m, new DateOnly(2025, 11, 1)), CriarLancamento(_conta, _mercado, 200m, new DateOnly(2025, 11, 2))],
            [CriarTransferencia(_conta, _inter, 100m, new DateOnly(2025, 11, 3)), CriarTransferencia(_inter, _conta, 50m, new DateOnly(2025, 11, 4))]);

        Assert.Equal(
            [TipoMovimentacaoEnum.Receita, TipoMovimentacaoEnum.Despesa, TipoMovimentacaoEnum.TransferenciaEnviada, TipoMovimentacaoEnum.TransferenciaRecebida],
            extrato.Movimentacoes.Select(m => m.Tipo));
        Assert.Equal([500m, -200m, -100m, 50m], extrato.Movimentacoes.Select(m => m.ValorComSinal));
    }

    [Fact]
    public void ConsolidarOrdenaPorDataEDepoisPorCriacao()
    {
        var extrato = CriarExtrato(new DateOnly(2025, 11, 1), new DateOnly(2025, 11, 30));
        var primeiro = CriarLancamento(_conta, _mercado, 10m, new DateOnly(2025, 11, 10));
        var segundo = CriarTransferencia(_conta, _inter, 20m, new DateOnly(2025, 11, 10));
        var maisAntigo = CriarLancamento(_conta, _salario, 30m, new DateOnly(2025, 11, 5));

        extrato.Consolidar([primeiro, maisAntigo], [segundo]);

        Assert.Equal([maisAntigo.Id, primeiro.Id, segundo.Id], extrato.Movimentacoes.Select(m => m.Id));
    }

    [Fact]
    public void ConsolidarCalculaSaldoAposETotais()
    {
        var extrato = CriarExtrato(new DateOnly(2025, 11, 1), new DateOnly(2025, 11, 30));

        extrato.Consolidar(
            [
                CriarLancamento(_conta, _salario, 5000m, new DateOnly(2025, 11, 5)),
                CriarLancamento(_conta, _mercado, 800m, new DateOnly(2025, 11, 10)),
                CriarLancamento(_conta, _mercado, 200m, new DateOnly(2025, 10, 10))
            ],
            [CriarTransferencia(_conta, _inter, 1000m, new DateOnly(2025, 11, 15))]);

        Assert.Equal(800m, extrato.SaldoAnterior);
        Assert.Equal(5000m, extrato.TotalEntradas);
        Assert.Equal(1800m, extrato.TotalSaidas);
        Assert.Equal(4000m, extrato.SaldoFinal);
        Assert.Equal([5800m, 5000m, 4000m], extrato.Movimentacoes.Select(m => m.SaldoApos));
        Assert.Equal(extrato.SaldoFinal, extrato.Movimentacoes[^1].SaldoApos);
        Assert.Equal(extrato.SaldoFinal, extrato.SaldoAnterior + extrato.TotalEntradas - extrato.TotalSaidas);
    }

    [Fact]
    public void ConsolidarIgnoraMovimentacoesDepoisDoPeriodoEDeOutrasContas()
    {
        var extrato = CriarExtrato(new DateOnly(2025, 10, 1), new DateOnly(2025, 10, 31));

        extrato.Consolidar(
            [CriarLancamento(_conta, _salario, 100m, new DateOnly(2025, 11, 5)), CriarLancamento(_inter, _salario, 100m, new DateOnly(2025, 10, 5))],
            []);

        Assert.Equal(1000m, extrato.SaldoAnterior);
        Assert.Empty(extrato.Movimentacoes);
        Assert.Equal(1000m, extrato.SaldoFinal);
    }
}
