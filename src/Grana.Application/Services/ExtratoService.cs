using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Services;

public sealed class ExtratoService(IContaRepository contaRepository, ILancamentoRepository lancamentoRepository, ITransferenciaRepository transferenciaRepository) : IExtratoService
{
    public async Task<ExtratoConta> ObterExtratoConta(Guid idConta, ExtratoContaDto dto, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var conta = await contaRepository.ObterContaPorId(idConta, cancellationToken);

        if (conta is null)
        {
            notificacao.AdicionarNaoEncontrado("Conta não encontrada.");
            return null;
        }

        var extrato = ExtratoConta.CriarExtratoConta(conta, dto.dataInicio, dto.dataFim, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        var lancamentos = await lancamentoRepository.ListarLancamentosDaContaAte(conta.Id, extrato.DataFim, cancellationToken);
        var filtro = FiltroTransferencia.CriarFiltro(conta.Id, null, extrato.DataFim, notificacao);
        var transferencias = await transferenciaRepository.ListarTransferencias(filtro, cancellationToken);

        extrato.Consolidar(lancamentos, transferencias);

        return extrato;
    }
}
