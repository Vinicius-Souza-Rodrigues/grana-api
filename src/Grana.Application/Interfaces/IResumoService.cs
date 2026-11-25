using Application.Dtos;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Interfaces;

public interface IResumoService
{
    Task<ResumoMensal> ObterResumoMensal(ResumoMensalDto dto, Notificacao notificacao, CancellationToken cancellationToken);
}
