using Application.Interfaces;
using Grana.Domain.Notificacoes;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Data;

public sealed class UnitOfWork(GranaDbContext contexto) : IUnitOfWork
{
    public async Task Salvar(Notificacao notificacao, CancellationToken cancellationToken)
    {
        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            notificacao.AdicionarConflito("A conta foi alterada por outra operação. Tente novamente.");
        }
    }
}
