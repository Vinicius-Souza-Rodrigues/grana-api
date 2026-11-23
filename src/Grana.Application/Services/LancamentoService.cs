using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Services;

public sealed class LancamentoService(ILancamentoRepository lancamentoRepository, IContaRepository contaRepository, ICategoriaRepository categoriaRepository) : ILancamentoService
{
    public async Task<Lancamento> CriarLancamento(LancamentoCriarDto dto, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var conta = await contaRepository.ObterContaPorId(dto.contaId, cancellationToken);

        if (conta is null)
            notificacao.AdicionarNaoEncontrado("Conta não encontrada.");

        var categoria = await categoriaRepository.ObterCategoriaPorId(dto.categoriaId, cancellationToken);

        if (categoria is null)
            notificacao.AdicionarNaoEncontrado("Categoria não encontrada.");

        if (notificacao.TemNotificacoes)
            return null;

        var lancamento = Lancamento.CriacaoLancamento(conta.Id, categoria, dto.valor, dto.data, dto.descricao, notificacao);

        conta.ValidarContaAtiva(notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        conta.AplicarLancamento(lancamento, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        var totalDespesasNoMes = await lancamentoRepository.SomarDespesasDaCategoriaNoMes(categoria.Id, lancamento.Data.Year, lancamento.Data.Month, cancellationToken);

        categoria.VerificarOrcamento(totalDespesasNoMes + lancamento.Valor, notificacao);

        await lancamentoRepository.AdicionarLancamento(lancamento, cancellationToken);

        return lancamento;
    }

    public async Task<Lancamento> ObterLancamentoPorId(Guid idLancamento, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var lancamento = await lancamentoRepository.ObterLancamentoPorId(idLancamento, cancellationToken);

        if (lancamento is null)
            notificacao.AdicionarNaoEncontrado("Lançamento não encontrado.");

        return lancamento;
    }
}
