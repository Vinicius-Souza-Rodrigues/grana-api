using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Services;

public sealed class TransferenciaService(ITransferenciaRepository transferenciaRepository, IContaRepository contaRepository) : ITransferenciaService
{
    public async Task<Transferencia> CriarTransferencia(TransferenciaCriarDto dto, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var contaOrigem = await contaRepository.ObterContaPorId(dto.contaOrigemId, cancellationToken);

        if (contaOrigem is null)
            notificacao.AdicionarNaoEncontrado("Conta de origem não encontrada.");

        var contaDestino = await contaRepository.ObterContaPorId(dto.contaDestinoId, cancellationToken);

        if (contaDestino is null)
            notificacao.AdicionarNaoEncontrado("Conta de destino não encontrada.");

        if (notificacao.TemNotificacoes)
            return null;

        var transferencia = Transferencia.CriacaoTransferencia(contaOrigem, contaDestino, dto.valor, dto.data, dto.descricao, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        transferencia.Efetivar(notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        await transferenciaRepository.AdicionarTransferencia(transferencia, cancellationToken);

        return transferencia;
    }

    public async Task<Transferencia> ObterTransferenciaPorId(Guid idTransferencia, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var transferencia = await transferenciaRepository.ObterTransferenciaPorId(idTransferencia, cancellationToken);

        if (transferencia is null)
            notificacao.AdicionarNaoEncontrado("Transferência não encontrada.");

        return transferencia;
    }

    public async Task ExcluirTransferencia(Guid idTransferencia, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var transferencia = await transferenciaRepository.ObterTransferenciaPorId(idTransferencia, cancellationToken);

        if (transferencia is null)
        {
            notificacao.AdicionarNaoEncontrado("Transferência não encontrada.");
            return;
        }

        transferencia.Estornar(notificacao);

        if (notificacao.TemNotificacoes)
            return;

        await transferenciaRepository.RemoverTransferencia(transferencia, cancellationToken);
    }

    public async Task<IReadOnlyList<Transferencia>> ListarTransferencias(TransferenciaListarDto dto, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var filtro = FiltroTransferencia.CriarFiltro(dto.contaId, dto.dataInicio, dto.dataFim, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        return await transferenciaRepository.ListarTransferencias(filtro, cancellationToken);
    }
}
