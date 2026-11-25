using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Tests.Domain;

public sealed class OrcamentoMensalTest
{
    private readonly Notificacao _notificacao = new();

    private static Categoria CriarCategoria(string nome, decimal? orcamentoMensal)
        => Categoria.CriacaoCategoria(nome, TipoCategoriaEnum.Despesa, orcamentoMensal, new Notificacao());

    [Fact]
    public void CriarOrcamentoMensalSemParametrosUsaOMesAtual()
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        var orcamentoMensal = OrcamentoMensal.CriarOrcamentoMensal(null, null, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(hoje.Year, orcamentoMensal.Ano);
        Assert.Equal(hoje.Month, orcamentoMensal.Mes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void CriarOrcamentoMensalComMesInvalido(int mes)
    {
        var orcamentoMensal = OrcamentoMensal.CriarOrcamentoMensal(2026, mes, _notificacao);

        Assert.Null(orcamentoMensal);
        Assert.Equal(["O mês deve estar entre 1 e 12."], _notificacao.Notificacoes);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void CriarOrcamentoMensalComAnoInvalido(int ano)
    {
        var orcamentoMensal = OrcamentoMensal.CriarOrcamentoMensal(ano, 11, _notificacao);

        Assert.Null(orcamentoMensal);
        Assert.Equal(["O ano deve estar entre 2000 e 2100."], _notificacao.Notificacoes);
    }

    [Theory]
    [InlineData(2000, 1)]
    [InlineData(2100, 12)]
    public void CriarOrcamentoMensalNosLimites(int ano, int mes)
    {
        var orcamentoMensal = OrcamentoMensal.CriarOrcamentoMensal(ano, mes, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(ano, orcamentoMensal.Ano);
        Assert.Equal(mes, orcamentoMensal.Mes);
    }

    [Fact]
    public void AdicionarCategoriasOrdenaPorPercentualDecrescenteESomaOsTotais()
    {
        var orcamentoMensal = OrcamentoMensal.CriarOrcamentoMensal(2026, 11, _notificacao);

        orcamentoMensal.AdicionarCategoria(CriarCategoria("Lazer", 200m), 50m);
        orcamentoMensal.AdicionarCategoria(CriarCategoria("Mercado", 400m), 320m);
        orcamentoMensal.AdicionarCategoria(CriarCategoria("Transporte", 100m), 150m);

        Assert.Equal(["Transporte", "Mercado", "Lazer"], orcamentoMensal.Categorias.Select(c => c.Categoria.Nome));
        Assert.Equal(700m, orcamentoMensal.TotalOrcado);
        Assert.Equal(520m, orcamentoMensal.TotalGasto);
    }

    [Fact]
    public void AdicionarCategoriaSemOrcamentoNaoEntra()
    {
        var orcamentoMensal = OrcamentoMensal.CriarOrcamentoMensal(2026, 11, _notificacao);

        orcamentoMensal.AdicionarCategoria(CriarCategoria("Farmácia", null), 90m);

        Assert.Empty(orcamentoMensal.Categorias);
        Assert.Equal(0m, orcamentoMensal.TotalOrcado);
        Assert.Equal(0m, orcamentoMensal.TotalGasto);
    }
}
