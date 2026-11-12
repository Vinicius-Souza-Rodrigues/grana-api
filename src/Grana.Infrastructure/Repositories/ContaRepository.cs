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

    public Task<bool> ExisteContaAtivaComNome(string nome, CancellationToken cancellationToken)
    {
        var existe = _contas.Exists(c =>
            c.StatusAtividadeConta == StatusAtividadeContaEnum.Ativo &&
            string.Equals(c.Nome, nome, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(existe);
    }
}
