using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Services;

public sealed class TransferenciaService(ITransferenciaRepository transferenciaRepository) : ITransferenciaService
{
    public async Task<Transferencia> ObterTransferenciaPorId(Guid idTransferencia, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var transferencia = await transferenciaRepository.ObterTransferenciaPorId(idTransferencia, cancellationToken);

        if (transferencia is null)
            notificacao.AdicionarNaoEncontrado("Transferência não encontrada.");

        return transferencia;
    }

    public async Task<IReadOnlyList<Transferencia>> ListarTransferencias(TransferenciaListarDto dto, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var filtro = FiltroTransferencia.CriarFiltro(dto.contaId, dto.dataInicio, dto.dataFim, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        return await transferenciaRepository.ListarTransferencias(filtro, cancellationToken);
    }
}
