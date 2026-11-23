using Application.Interfaces;
using Domain.Models;

namespace Infraestructure.Repositories;

public sealed class LancamentoRepository : ILancamentoRepository
{
    private List<Lancamento> _lancamentos = [];

    public Task AdicionarLancamento(Lancamento lancamento, CancellationToken cancellationToken)
    {
        _lancamentos.Add(lancamento);

        return Task.CompletedTask;
    }

    public Task<Lancamento> ObterLancamentoPorId(Guid idLancamento, CancellationToken cancellationToken)
    {
        var lancamento = _lancamentos.Find(l => l.Id.Equals(idLancamento));

        return Task.FromResult(lancamento);
    }

    public Task<bool> ExisteLancamentoComCategoria(Guid idCategoria, CancellationToken cancellationToken)
    {
        var existe = _lancamentos.Exists(l => l.IdCategoria == idCategoria);

        return Task.FromResult(existe);
    }
}
