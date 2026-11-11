using Grana.Domain.Notificacoes;

namespace Domain.Shared.Paginacao;

public sealed class PaginacaoPrincipal<T>
{
    private PaginacaoPrincipal(int pagina, int tamanhoPagina)
    {
        Pagina = pagina;
        TamanhoPagina = tamanhoPagina;
    }

    public const int PaginaPadrao = 1;
    public const int TamanhoPadrao = 20;
    public const int TamanhoMaximo = 100;

    public int Pagina { get; private set; }
    public int TamanhoPagina { get; private set; }
    public IReadOnlyList<T> Itens { get; private set; } = [];
    public int TotalItens { get; private set; }
    public int TotalPaginas => (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);

    public static PaginacaoPrincipal<T> CriarPaginacao(int? pagina, int? tamanhoPagina, Notificacao notificacao)
    {
        var paginaFinal = pagina;
        var tamanhoFinal = tamanhoPagina;

        if (paginaFinal == null)
            paginaFinal = PaginaPadrao;

        if (tamanhoFinal == null)
            tamanhoFinal = TamanhoPadrao;

        if (paginaFinal < 1)
            notificacao.Adicionar("A página deve ser maior ou igual a 1.");

        if (tamanhoFinal < 1 || tamanhoFinal > TamanhoMaximo)
            notificacao.Adicionar("O tamanho da página deve estar entre 1 e 100.");

        if (notificacao.TemNotificacoes)
            return null;

        return new PaginacaoPrincipal<T>(paginaFinal.Value, tamanhoFinal.Value);
    }

    public void PreencherPaginacao(IReadOnlyList<T> itens, int totalItens)
    {
        Itens = itens;
        TotalItens = totalItens;
    }
}
