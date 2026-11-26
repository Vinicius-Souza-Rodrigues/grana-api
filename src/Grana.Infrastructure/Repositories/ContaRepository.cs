using Application.Interfaces;
using Domain.Enums;
using Domain.Models;
using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories;

public sealed class ContaRepository(GranaDbContext contexto) : IContaRepository
{
    public async Task AdicionarConta(Conta conta, CancellationToken cancellationToken)
    {
        await contexto.Contas.AddAsync(conta, cancellationToken);
    }

    public Task<Conta> ObterContaPorId(Guid idConta, CancellationToken cancellationToken)
    {
        return contexto.Contas.FirstOrDefaultAsync(c => c.Id == idConta, cancellationToken);
    }

    public Task<bool> ExisteContaAtivaComNome(string nome, Guid idContaIgnorada, CancellationToken cancellationToken)
    {
        var nomeNormalizado = nome?.ToLower();

        return contexto.Contas.AnyAsync(c =>
            c.Id != idContaIgnorada &&
            c.StatusAtividadeConta == StatusAtividadeContaEnum.Ativo &&
            c.Nome.ToLower() == nomeNormalizado,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Conta>> ListarContas(bool incluirInativas, CancellationToken cancellationToken)
    {
        return await contexto.Contas
            .Where(c => incluirInativas || c.StatusAtividadeConta == StatusAtividadeContaEnum.Ativo)
            .OrderBy(c => c.Nome)
            .ToListAsync(cancellationToken);
    }
}
