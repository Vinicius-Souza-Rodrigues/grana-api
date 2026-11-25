using Domain.Models;
using Domain.Shared.Paginacao;

namespace Application.Interfaces;

public interface ILancamentoRepository
{
    Task AdicionarLancamento(Lancamento lancamento, CancellationToken cancellationToken);

    Task<Lancamento> ObterLancamentoPorId(Guid idLancamento, CancellationToken cancellationToken);

    Task RemoverLancamento(Lancamento lancamento, CancellationToken cancellationToken);

    Task ListarLancamentos(FiltroLancamento filtro, PaginacaoPrincipal<Lancamento> paginacao, CancellationToken cancellationToken);

    Task<bool> ExisteLancamentoComCategoria(Guid idCategoria, CancellationToken cancellationToken);

    Task<IReadOnlyList<Lancamento>> ListarLancamentosDoMes(int ano, int mes, CancellationToken cancellationToken);

    Task<IReadOnlyList<Lancamento>> ListarLancamentosDaContaAte(Guid idConta, DateOnly dataFim, CancellationToken cancellationToken);

    Task<decimal> SomarDespesasDaCategoriaNoMes(Guid idCategoria, int ano, int mes, CancellationToken cancellationToken);
}
