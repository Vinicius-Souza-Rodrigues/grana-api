using Application.Interfaces;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Services;

public sealed class LancamentoService(ILancamentoRepository lancamentoRepository) : ILancamentoService
{
    public async Task<Lancamento> ObterLancamentoPorId(Guid idLancamento, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var lancamento = await lancamentoRepository.ObterLancamentoPorId(idLancamento, cancellationToken);

        if (lancamento is null)
            notificacao.AdicionarNaoEncontrado("Lançamento não encontrado.");

        return lancamento;
    }
}
