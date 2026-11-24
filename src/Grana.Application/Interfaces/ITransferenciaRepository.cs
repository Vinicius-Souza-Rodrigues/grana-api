using Domain.Models;

namespace Application.Interfaces;

public interface ITransferenciaRepository
{
    Task AdicionarTransferencia(Transferencia transferencia, CancellationToken cancellationToken);

    Task<Transferencia> ObterTransferenciaPorId(Guid idTransferencia, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transferencia>> ListarTransferencias(FiltroTransferencia filtro, CancellationToken cancellationToken);
}
