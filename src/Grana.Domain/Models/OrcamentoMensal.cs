using Grana.Domain.Notificacoes;

namespace Domain.Models;

public sealed class OrcamentoMensal
{
    private readonly List<OrcamentoCategoria> _categorias = [];

    private OrcamentoMensal(int ano, int mes)
    {
        Ano = ano;
        Mes = mes;
    }

    public int Ano { get; private set; }
    public int Mes { get; private set; }

    public IReadOnlyList<OrcamentoCategoria> Categorias => _categorias
        .OrderByDescending(c => c.PercentualUsado)
        .ThenBy(c => c.Categoria.Nome)
        .ToList();

    public decimal TotalOrcado => _categorias.Sum(c => c.Limite);
    public decimal TotalGasto => _categorias.Sum(c => c.Gasto);

    public const int AnoMinimo = 2000;
    public const int AnoMaximo = 2100;

    public static OrcamentoMensal CriarOrcamentoMensal(int? ano, int? mes, Notificacao notificacao)
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

        return new OrcamentoMensal(anoConsulta, mesConsulta);
    }

    public void AdicionarCategoria(Categoria categoria, decimal gasto)
    {
        var orcamentoCategoria = OrcamentoCategoria.CalcularOrcamento(categoria, gasto);

        if (orcamentoCategoria is null)
            return;

        _categorias.Add(orcamentoCategoria);
    }
}
