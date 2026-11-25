using Application.Dtos;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Interfaces;

public interface IExtratoService
{
    Task<ExtratoConta> ObterExtratoConta(Guid idConta, ExtratoContaDto dto, Notificacao notificacao, CancellationToken cancellationToken);
}
