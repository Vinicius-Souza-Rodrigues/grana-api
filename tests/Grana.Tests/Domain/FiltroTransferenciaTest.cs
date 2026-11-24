using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Tests.Domain;

public sealed class FiltroTransferenciaTest
{
    private readonly Notificacao _notificacao = new();

    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void CriarFiltroComDataInicioMaiorQueDataFim()
    {
        var filtro = FiltroTransferencia.CriarFiltro(null, Hoje, Hoje.AddDays(-1), _notificacao);

        Assert.Null(filtro);
        Assert.Equal(["A data de início não pode ser maior que a data de fim."], _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarFiltroComDataInicioIgualADataFim()
    {
        var filtro = FiltroTransferencia.CriarFiltro(null, Hoje, Hoje, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(Hoje, filtro.DataInicio);
        Assert.Equal(Hoje, filtro.DataFim);
    }

    [Fact]
    public void CriarFiltroComConta()
    {
        var idConta = Guid.NewGuid();

        var filtro = FiltroTransferencia.CriarFiltro(idConta, null, null, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(idConta, filtro.IdConta);
    }
}
