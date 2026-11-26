using Grana.Domain.Notificacoes;

namespace Application.Interfaces;

public interface IUnitOfWork
{
    Task Salvar(Notificacao notificacao, CancellationToken cancellationToken);
}
