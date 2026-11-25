using Application.Dtos;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Interfaces;

public interface ITransferenciaService
{
    Task<Transferencia> CriarTransferencia(TransferenciaCriarDto dto, Notificacao notificacao, CancellationToken cancellationToken);

    Task<Transferencia> ObterTransferenciaPorId(Guid idTransferencia, Notificacao notificacao, CancellationToken cancellationToken);

    Task ExcluirTransferencia(Guid idTransferencia, Notificacao notificacao, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transferencia>> ListarTransferencias(TransferenciaListarDto dto, Notificacao notificacao, CancellationToken cancellationToken);
}
