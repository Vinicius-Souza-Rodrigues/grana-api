namespace Domain.Models;

public sealed class TotalPorCategoria
{
    public TotalPorCategoria(Categoria categoria, decimal total, decimal percentual)
    {
        Categoria = categoria;
        Total = total;
        Percentual = percentual;
    }

    public Categoria Categoria { get; private set; }
    public decimal Total { get; private set; }
    public decimal Percentual { get; private set; }
}
