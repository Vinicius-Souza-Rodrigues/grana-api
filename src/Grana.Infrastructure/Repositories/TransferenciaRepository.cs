using Application.Interfaces;
using Domain.Models;
using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories;

public sealed class TransferenciaRepository(GranaDbContext contexto) : ITransferenciaRepository
{
    private IQueryable<Transferencia> TransferenciasCompletas => contexto.Transferencias
        .Include(t => t.ContaOrigem)
        .Include(t => t.ContaDestino);

    public async Task AdicionarTransferencia(Transferencia transferencia, CancellationToken cancellationToken)
    {
        await contexto.Transferencias.AddAsync(transferencia, cancellationToken);
    }

    public Task<Transferencia> ObterTransferenciaPorId(Guid idTransferencia, CancellationToken cancellationToken)
    {
        return TransferenciasCompletas.FirstOrDefaultAsync(t => t.Id == idTransferencia, cancellationToken);
    }

    public Task RemoverTransferencia(Transferencia transferencia, CancellationToken cancellationToken)
    {
        contexto.Transferencias.Remove(transferencia);

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Transferencia>> ListarTransferencias(FiltroTransferencia filtro, CancellationToken cancellationToken)
    {
        return await TransferenciasCompletas
            .Where(t =>
                (filtro.IdConta == null || t.IdContaOrigem == filtro.IdConta || t.IdContaDestino == filtro.IdConta) &&
                (filtro.DataInicio == null || t.Data >= filtro.DataInicio) &&
                (filtro.DataFim == null || t.Data <= filtro.DataFim))
            .OrderByDescending(t => t.Data)
            .ThenByDescending(t => t.CriadoEm)
            .ToListAsync(cancellationToken);
    }
}
