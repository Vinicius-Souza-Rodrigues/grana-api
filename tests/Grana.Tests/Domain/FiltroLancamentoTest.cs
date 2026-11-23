using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Tests.Domain;

public sealed class FiltroLancamentoTest
{
    private readonly Notificacao _notificacao = new();

    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void CriarFiltroComDataInicioMaiorQueDataFim()
    {
        var filtro = FiltroLancamento.CriarFiltro(null, null, null, Hoje, Hoje.AddDays(-1), null, _notificacao);

        Assert.Null(filtro);
        Assert.Contains("A data de início não pode ser maior que a data de fim.", _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarFiltroComDataInicioIgualADataFim()
    {
        var filtro = FiltroLancamento.CriarFiltro(null, null, null, Hoje, Hoje, null, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(Hoje, filtro.DataInicio);
        Assert.Equal(Hoje, filtro.DataFim);
    }

    [Fact]
    public void CriarFiltroSomenteComDataInicio()
    {
        var filtro = FiltroLancamento.CriarFiltro(null, null, null, Hoje, null, null, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Null(filtro.DataFim);
    }

    [Fact]
    public void CriarFiltroComTodosOsCampos()
    {
        var idConta = Guid.NewGuid();
        var idCategoria = Guid.NewGuid();

        var filtro = FiltroLancamento.CriarFiltro(idConta, idCategoria, TipoCategoriaEnum.Despesa, Hoje.AddDays(-10), Hoje, "  mercado  ", _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(idConta, filtro.IdConta);
        Assert.Equal(idCategoria, filtro.IdCategoria);
        Assert.Equal(TipoCategoriaEnum.Despesa, filtro.TipoLancamento);
        Assert.Equal("mercado", filtro.Busca);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void CriarFiltroSemBusca(string busca)
    {
        var filtro = FiltroLancamento.CriarFiltro(null, null, null, null, null, busca, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Null(filtro.Busca);
    }
}
