using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Interfaces;

public interface ILancamentoService
{
    Task<Lancamento> ObterLancamentoPorId(Guid idLancamento, Notificacao notificacao, CancellationToken cancellationToken);
}
