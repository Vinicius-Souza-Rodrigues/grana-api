using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Tests.Domain;

public sealed class OrcamentoCategoriaTest
{
    private static Categoria CriarCategoria(TipoCategoriaEnum tipo, decimal? orcamentoMensal)
        => Categoria.CriacaoCategoria("Mercado", tipo, orcamentoMensal, new Notificacao());

    [Fact]
    public void CalcularOrcamentoComGastoDe80Porcento()
    {
        var orcamento = OrcamentoCategoria.CalcularOrcamento(CriarCategoria(TipoCategoriaEnum.Despesa, 400m), 320m);

        Assert.Equal(400m, orcamento.Limite);
        Assert.Equal(320m, orcamento.Gasto);
        Assert.Equal(80m, orcamento.Disponivel);
        Assert.Equal(80.00m, orcamento.PercentualUsado);
        Assert.Equal(SituacaoOrcamentoEnum.Alerta, orcamento.Situacao);
    }

    [Fact]
    public void CalcularOrcamentoAbaixoDe80Porcento()
    {
        var orcamento = OrcamentoCategoria.CalcularOrcamento(CriarCategoria(TipoCategoriaEnum.Despesa, 400m), 319.99m);

        Assert.Equal(SituacaoOrcamentoEnum.Dentro, orcamento.Situacao);
    }

    [Fact]
    public void CalcularOrcamentoNoLimite()
    {
        var orcamento = OrcamentoCategoria.CalcularOrcamento(CriarCategoria(TipoCategoriaEnum.Despesa, 400m), 400m);

        Assert.Equal(100.00m, orcamento.PercentualUsado);
        Assert.Equal(0m, orcamento.Disponivel);
        Assert.Equal(SituacaoOrcamentoEnum.Alerta, orcamento.Situacao);
    }

    [Fact]
    public void CalcularOrcamentoEstourado()
    {
        var orcamento = OrcamentoCategoria.CalcularOrcamento(CriarCategoria(TipoCategoriaEnum.Despesa, 400m), 500m);

        Assert.Equal(125.00m, orcamento.PercentualUsado);
        Assert.Equal(-100m, orcamento.Disponivel);
        Assert.Equal(SituacaoOrcamentoEnum.Estourado, orcamento.Situacao);
    }

    [Fact]
    public void CalcularOrcamentoSemGasto()
    {
        var orcamento = OrcamentoCategoria.CalcularOrcamento(CriarCategoria(TipoCategoriaEnum.Despesa, 400m), 0m);

        Assert.Equal(0m, orcamento.PercentualUsado);
        Assert.Equal(400m, orcamento.Disponivel);
        Assert.Equal(SituacaoOrcamentoEnum.Dentro, orcamento.Situacao);
    }

    [Fact]
    public void CalcularOrcamentoArredondaPercentualParaCima()
    {
        var orcamento = OrcamentoCategoria.CalcularOrcamento(CriarCategoria(TipoCategoriaEnum.Despesa, 800m), 0.04m);

        Assert.Equal(0.01m, orcamento.PercentualUsado);
    }

    [Fact]
    public void CalcularOrcamentoDeCategoriaSemOrcamento()
    {
        var orcamento = OrcamentoCategoria.CalcularOrcamento(CriarCategoria(TipoCategoriaEnum.Despesa, null), 100m);

        Assert.Null(orcamento);
    }

    [Fact]
    public void CalcularOrcamentoDeCategoriaDeReceita()
    {
        var orcamento = OrcamentoCategoria.CalcularOrcamento(CriarCategoria(TipoCategoriaEnum.Receita, null), 100m);

        Assert.Null(orcamento);
    }
}
