using Application.Dtos;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Interfaces;

public interface IOrcamentoService
{
    Task<OrcamentoMensal> ListarOrcamentos(OrcamentoListarDto dto, Notificacao notificacao, CancellationToken cancellationToken);
}
