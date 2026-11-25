using Domain.Enums;
using Grana.Domain.Notificacoes;

namespace Domain.Models;

public sealed class ResumoMensal
{
    private ResumoMensal(int ano, int mes)
    {
        Ano = ano;
        Mes = mes;

        var inicioDoMesAnterior = new DateOnly(ano, mes, 1).AddMonths(-1);
        AnoAnterior = inicioDoMesAnterior.Year;
        MesAnterior = inicioDoMesAnterior.Month;
    }

    public int Ano { get; private set; }
    public int Mes { get; private set; }
    public int AnoAnterior { get; private set; }
    public int MesAnterior { get; private set; }
    public decimal TotalReceitas { get; private set; }
    public decimal TotalDespesas { get; private set; }
    public decimal ResultadoDoMes => TotalReceitas - TotalDespesas;
    public decimal SaldoAtualContas { get; private set; }
    public decimal? ComparativoReceitas { get; private set; }
    public decimal? ComparativoDespesas { get; private set; }
    public IReadOnlyList<TotalPorCategoria> DespesasPorCategoria { get; private set; } = [];
    public IReadOnlyList<TotalPorCategoria> ReceitasPorCategoria { get; private set; } = [];

    public const int AnoMinimo = 2000;
    public const int AnoMaximo = 2100;

    public static ResumoMensal CriarResumoMensal(int? ano, int? mes, Notificacao notificacao)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        var anoConsulta = ano ?? hoje.Year;
        var mesConsulta = mes ?? hoje.Month;

        if (mesConsulta < 1 || mesConsulta > 12)
            notificacao.Adicionar("O mês deve estar entre 1 e 12.");

        if (anoConsulta < AnoMinimo || anoConsulta > AnoMaximo)
            notificacao.Adicionar($"O ano deve estar entre {AnoMinimo} e {AnoMaximo}.");

        if (notificacao.TemNotificacoes)
            return null;

        return new ResumoMensal(anoConsulta, mesConsulta);
    }

    public void Consolidar(IReadOnlyList<Lancamento> lancamentosDoMes, IReadOnlyList<Lancamento> lancamentosDoMesAnterior, IReadOnlyList<Conta> contas)
    {
        var despesasDoMes = lancamentosDoMes.Where(l => l.TipoLancamento == TipoCategoriaEnum.Despesa).ToList();
        var receitasDoMes = lancamentosDoMes.Where(l => l.TipoLancamento == TipoCategoriaEnum.Receita).ToList();

        TotalReceitas = receitasDoMes.Sum(l => l.Valor);
        TotalDespesas = despesasDoMes.Sum(l => l.Valor);

        var receitasDoMesAnterior = lancamentosDoMesAnterior.Where(l => l.TipoLancamento == TipoCategoriaEnum.Receita).Sum(l => l.Valor);
        var despesasDoMesAnterior = lancamentosDoMesAnterior.Where(l => l.TipoLancamento == TipoCategoriaEnum.Despesa).Sum(l => l.Valor);

        ComparativoReceitas = CalcularVariacao(receitasDoMesAnterior, TotalReceitas);
        ComparativoDespesas = CalcularVariacao(despesasDoMesAnterior, TotalDespesas);

        SaldoAtualContas = contas
            .Where(c => c.StatusAtividadeConta == StatusAtividadeContaEnum.Ativo)
            .Sum(c => c.Saldo);

        DespesasPorCategoria = AgruparPorCategoria(despesasDoMes, TotalDespesas);
        ReceitasPorCategoria = AgruparPorCategoria(receitasDoMes, TotalReceitas);
    }

    private static decimal? CalcularVariacao(decimal valorAnterior, decimal valorAtual)
    {
        if (valorAnterior == 0)
            return null;

        return decimal.Round((valorAtual - valorAnterior) / valorAnterior * 100, 2, MidpointRounding.AwayFromZero);
    }

    private static IReadOnlyList<TotalPorCategoria> AgruparPorCategoria(List<Lancamento> lancamentos, decimal totalGeral)
    {
        var grupos = lancamentos
            .GroupBy(l => l.IdCategoria)
            .Select(g => new { Categoria = g.First().Categoria, Total = g.Sum(l => l.Valor) })
            .OrderByDescending(g => g.Total)
            .ThenBy(g => g.Categoria.Nome)
            .ToList();

        if (grupos.Count == 0)
            return [];

        var percentuais = grupos
            .Select(g => decimal.Round(g.Total / totalGeral * 100, 2, MidpointRounding.AwayFromZero))
            .ToList();

        percentuais[0] += 100 - percentuais.Sum();

        return grupos
            .Select((g, indice) => new TotalPorCategoria(g.Categoria, g.Total, percentuais[indice]))
            .ToList();
    }
}
