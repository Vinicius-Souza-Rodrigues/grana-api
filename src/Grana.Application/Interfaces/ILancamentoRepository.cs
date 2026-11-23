using Domain.Models;

namespace Application.Interfaces;

public interface ILancamentoRepository
{
    Task AdicionarLancamento(Lancamento lancamento, CancellationToken cancellationToken);

    Task<Lancamento> ObterLancamentoPorId(Guid idLancamento, CancellationToken cancellationToken);

    Task<bool> ExisteLancamentoComCategoria(Guid idCategoria, CancellationToken cancellationToken);

    Task<decimal> SomarDespesasDaCategoriaNoMes(Guid idCategoria, int ano, int mes, CancellationToken cancellationToken);
}
