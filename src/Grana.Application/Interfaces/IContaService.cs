using Application.Dtos;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Interfaces;

public interface IContaService
{
    Task<Conta> CriarConta(ContaCriarDto dto, Notificacao notificacao, CancellationToken cancellationToken);
    
    Task<Conta> ObterContaPorId(Guid idConta, Notificacao notificacao, CancellationToken cancellationToken);

    Task<IReadOnlyList<Conta>> ListarContas(bool incluirInativas, CancellationToken cancellationToken);

    Task<Conta> RenomearConta(Guid idConta, ContaRenomearDto dto, Notificacao notificacao, CancellationToken cancellationToken);

    Task<Conta> DesativarConta(Guid idConta, Notificacao notificacao, CancellationToken cancellationToken);

    Task<Conta> ReativarConta(Guid idConta, Notificacao notificacao, CancellationToken cancellationToken);

    Task<Notificacao> CreditarConta(Guid idConta, decimal valorCredito, CancellationToken cancellationToken);

    Task<Notificacao> DebitarConta(Guid idConta, decimal valorCredito, CancellationToken cancellationToken);
}
