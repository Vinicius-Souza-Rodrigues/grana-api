namespace Application.Interfaces;

public interface IUnitOfWork
{
    Task Salvar(CancellationToken cancellationToken);
}
