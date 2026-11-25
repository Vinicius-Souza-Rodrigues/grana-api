using Domain.Enums;
using Grana.Domain.Notificacoes;

namespace Domain.Models;

public sealed class ExtratoConta
{
    private ExtratoConta(Conta conta, DateOnly dataInicio, DateOnly dataFim)
    {
        Conta = conta;
        DataInicio = dataInicio;
        DataFim = dataFim;
    }

    public Conta Conta { get; private set; }
    public DateOnly DataInicio { get; private set; }
    public DateOnly DataFim { get; private set; }
    public decimal SaldoAnterior { get; private set; }
    public decimal TotalEntradas { get; private set; }
    public decimal TotalSaidas { get; private set; }
    public decimal SaldoFinal => SaldoAnterior + TotalEntradas - TotalSaidas;
    public IReadOnlyList<MovimentacaoExtrato> Movimentacoes { get; private set; } = [];

    public const int PeriodoMaximoEmDias = 366;

    public static ExtratoConta CriarExtratoConta(Conta conta, DateOnly? dataInicio, DateOnly? dataFim, Notificacao notificacao)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicioDoMes = new DateOnly(hoje.Year, hoje.Month, 1);

        var inicio = dataInicio ?? inicioDoMes;
        var fim = dataFim ?? inicioDoMes.AddMonths(1).AddDays(-1);

        if (conta is null)
            notificacao.Adicionar("A conta do extrato é obrigatória.");

        if (inicio > fim)
            notificacao.Adicionar("A data de início não pode ser maior que a data de fim.");

        else if (fim.DayNumber - inicio.DayNumber + 1 > PeriodoMaximoEmDias)
            notificacao.Adicionar($"O período do extrato não pode ter mais que {PeriodoMaximoEmDias} dias.");

        if (notificacao.TemNotificacoes)
            return null;

        return new ExtratoConta(conta, inicio, fim);
    }

    public void Consolidar(IReadOnlyList<Lancamento> lancamentos, IReadOnlyList<Transferencia> transferencias)
    {
        var movimentacoes = lancamentos
            .Where(l => l.IdConta == Conta.Id && l.Data <= DataFim)
            .Select(CriarMovimentacao)
            .Concat(transferencias
                .Where(t => (t.IdContaOrigem == Conta.Id || t.IdContaDestino == Conta.Id) && t.Data <= DataFim)
                .Select(CriarMovimentacao))
            .ToList();

        SaldoAnterior = Conta.SaldoInicial + movimentacoes
            .Where(m => m.Data < DataInicio)
            .Sum(m => m.ValorComSinal);

        var doPeriodo = movimentacoes
            .Where(m => m.Data >= DataInicio)
            .OrderBy(m => m.Data)
            .ThenBy(m => m.CriadoEm)
            .ToList();

        TotalEntradas = doPeriodo.Where(m => m.EhEntrada).Sum(m => m.Valor);
        TotalSaidas = doPeriodo.Where(m => !m.EhEntrada).Sum(m => m.Valor);

        var saldo = SaldoAnterior;

        foreach (var movimentacao in doPeriodo)
        {
            saldo += movimentacao.ValorComSinal;
            movimentacao.DefinirSaldoApos(saldo);
        }

        Movimentacoes = doPeriodo;
    }

    private static MovimentacaoExtrato CriarMovimentacao(Lancamento lancamento)
    {
        TipoMovimentacaoEnum tipo;

        switch (lancamento.TipoLancamento)
        {
            case TipoCategoriaEnum.Receita:
                tipo = TipoMovimentacaoEnum.Receita;
                break;
            default:
                tipo = TipoMovimentacaoEnum.Despesa;
                break;
        }

        return new MovimentacaoExtrato(lancamento.Id, tipo, lancamento.Valor, lancamento.Data, lancamento.Descricao, lancamento.CriadoEm);
    }

    private MovimentacaoExtrato CriarMovimentacao(Transferencia transferencia)
    {
        var tipo = transferencia.IdContaOrigem == Conta.Id
            ? TipoMovimentacaoEnum.TransferenciaEnviada
            : TipoMovimentacaoEnum.TransferenciaRecebida;

        return new MovimentacaoExtrato(transferencia.Id, tipo, transferencia.Valor, transferencia.Data, transferencia.Descricao, transferencia.CriadoEm);
    }
}
