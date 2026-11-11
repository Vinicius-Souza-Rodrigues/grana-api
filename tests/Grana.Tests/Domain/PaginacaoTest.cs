using Domain.Shared.Paginacao;
using Grana.Domain.Notificacoes;

namespace Tests.Domain;

public sealed class PaginacaoTest
{
    private readonly Notificacao _notificacao = new();

    [Fact]
    public void TotalPaginasArredondaParaCima()
    {
        var paginacao = PaginacaoPrincipal<int>.CriarPaginacao(1, 20, new Notificacao());

        paginacao.PreencherPaginacao([], 57);

        Assert.Equal(3, paginacao.TotalPaginas);
    }

    [Fact]
    public void TamanhoMenorQueUm()
    {
        var paginacao = PaginacaoPrincipal<int>.CriarPaginacao(1, 0, _notificacao);

        Assert.Null(paginacao);
        Assert.True(_notificacao.TemNotificacoes);
    }

    [Fact]
    public void TamanhoMaiorQueCem()
    {
        var paginacao = PaginacaoPrincipal<int>.CriarPaginacao(1, 101, _notificacao);

        Assert.Null(paginacao);
        Assert.True(_notificacao.TemNotificacoes);
    }

    [Fact]
    public void SemParametrosUsaPadrao()
    {
        var paginacao = PaginacaoPrincipal<int>.CriarPaginacao(null, null, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(1, paginacao.Pagina);
        Assert.Equal(20, paginacao.TamanhoPagina);
    }
}
