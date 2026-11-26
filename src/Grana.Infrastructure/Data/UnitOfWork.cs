using Application.Interfaces;

namespace Infraestructure.Data;

public sealed class UnitOfWork(GranaDbContext contexto) : IUnitOfWork
{
    public Task Salvar(CancellationToken cancellationToken)
    {
        return contexto.SaveChangesAsync(cancellationToken);
    }
}
