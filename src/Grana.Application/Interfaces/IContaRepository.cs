using Domain.Models;

namespace Application.Interfaces;

public interface IContaRepository
{
    Task AdicionarConta(Conta conta, CancellationToken cancellationToken);

    Task<Conta> ObterContaPorId(Guid idConta, CancellationToken cancellationToken);

    Task<bool> ExisteContaAtivaComNome(string nome, CancellationToken cancellationToken);

    Task<IReadOnlyList<Conta>> ListarContas(bool incluirInativas, CancellationToken cancellationToken);
}
