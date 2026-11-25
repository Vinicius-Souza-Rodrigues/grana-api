using Application.Interfaces;
using Domain.Models;

namespace Infraestructure.Repositories;

public sealed class TransferenciaRepository : ITransferenciaRepository
{
    private List<Transferencia> _transferencias = [];

    public Task AdicionarTransferencia(Transferencia transferencia, CancellationToken cancellationToken)
    {
        _transferencias.Add(transferencia);

        return Task.CompletedTask;
    }

    public Task<Transferencia> ObterTransferenciaPorId(Guid idTransferencia, CancellationToken cancellationToken)
    {
        var transferencia = _transferencias.Find(t => t.Id.Equals(idTransferencia));

        return Task.FromResult(transferencia);
    }

    public Task RemoverTransferencia(Transferencia transferencia, CancellationToken cancellationToken)
    {
        _transferencias.Remove(transferencia);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Transferencia>> ListarTransferencias(FiltroTransferencia filtro, CancellationToken cancellationToken)
    {
        IReadOnlyList<Transferencia> transferencias = _transferencias
            .Where(t =>
                (filtro.IdConta is null || t.IdContaOrigem == filtro.IdConta || t.IdContaDestino == filtro.IdConta) &&
                (filtro.DataInicio is null || t.Data >= filtro.DataInicio) &&
                (filtro.DataFim is null || t.Data <= filtro.DataFim))
            .OrderByDescending(t => t.Data)
            .ThenByDescending(t => t.CriadoEm)
            .ToList();

        return Task.FromResult(transferencias);
    }
}
