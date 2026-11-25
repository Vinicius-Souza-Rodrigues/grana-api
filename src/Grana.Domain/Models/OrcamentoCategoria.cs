using Domain.Enums;

namespace Domain.Models;

public sealed class OrcamentoCategoria
{
    private OrcamentoCategoria(Categoria categoria, decimal limite, decimal gasto)
    {
        Categoria = categoria;
        Limite = limite;
        Gasto = gasto;
        Disponivel = limite - gasto;
        PercentualUsado = decimal.Round(gasto / limite * 100, 2, MidpointRounding.AwayFromZero);
        Situacao = DefinirSituacao(limite, gasto);
    }

    public Categoria Categoria { get; private set; }
    public decimal Limite { get; private set; }
    public decimal Gasto { get; private set; }
    public decimal Disponivel { get; private set; }
    public decimal PercentualUsado { get; private set; }
    public SituacaoOrcamentoEnum Situacao { get; private set; }

    public static OrcamentoCategoria CalcularOrcamento(Categoria categoria, decimal gasto)
    {
        if (categoria.TipoCategoria != TipoCategoriaEnum.Despesa || categoria.OrcamentoMensal is null)
            return null;

        return new OrcamentoCategoria(categoria, categoria.OrcamentoMensal.Value, gasto);
    }

    private static SituacaoOrcamentoEnum DefinirSituacao(decimal limite, decimal gasto)
    {
        if (gasto > limite)
            return SituacaoOrcamentoEnum.Estourado;

        if (gasto >= limite * Categoria.PercentualAlertaOrcamento)
            return SituacaoOrcamentoEnum.Alerta;

        return SituacaoOrcamentoEnum.Dentro;
    }
}
