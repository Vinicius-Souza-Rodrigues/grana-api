using Application.Dtos;
using Domain.Models;
using Domain.Shared.Paginacao;
using Grana.Domain.Notificacoes;

namespace Application.Interfaces;

public interface ILancamentoService
{
    Task<Lancamento> CriarLancamento(LancamentoCriarDto dto, Notificacao notificacao, CancellationToken cancellationToken);

    Task<Lancamento> ObterLancamentoPorId(Guid idLancamento, Notificacao notificacao, CancellationToken cancellationToken);

    Task<PaginacaoPrincipal<Lancamento>> ListarLancamentos(LancamentoListarDto dto, Notificacao notificacao, CancellationToken cancellationToken);
}
