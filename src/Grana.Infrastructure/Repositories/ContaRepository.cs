using Application.Interfaces;
using Domain.Enums;
using Domain.Models;

namespace Infraestructure.Repositories;

public sealed class ContaRepository : IContaRepository
{
    private List<Conta> _contas = [];

    public Task AdicionarConta(Conta conta, CancellationToken cancellationToken)
    {
        _contas.Add(conta);

        return Task.CompletedTask;
    }
    
    public Task<Conta> ObterContaPorId(Guid idConta, CancellationToken cancellationToken)
    {
        var conta =_contas.Find(c => c.Id.Equals(idConta));
        
        return Task.FromResult(conta);
    }

    public Task<bool> ExisteContaAtivaComNome(string nome, Guid idContaIgnorada, CancellationToken cancellationToken)
    {
        var existe = _contas.Exists(c =>
            c.Id != idContaIgnorada &&
            c.StatusAtividadeConta == StatusAtividadeContaEnum.Ativo &&
            string.Equals(c.Nome, nome, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(existe);
    }

    public Task<IReadOnlyList<Conta>> ListarContas(bool incluirInativas, CancellationToken cancellationToken)
    {
        IReadOnlyList<Conta> contas = _contas
            .Where(c => incluirInativas || c.StatusAtividadeConta == StatusAtividadeContaEnum.Ativo)
            .OrderBy(c => c.Nome)
            .ToList();

        return Task.FromResult(contas);
    }
}
