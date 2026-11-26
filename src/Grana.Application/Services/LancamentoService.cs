using Application.Dtos;
using Application.Interfaces;
using Domain.Enums;
using Domain.Models;
using Domain.Shared.Paginacao;
using Grana.Domain.Notificacoes;

namespace Application.Services;

public sealed class LancamentoService(ILancamentoRepository lancamentoRepository, IContaRepository contaRepository, ICategoriaRepository categoriaRepository, IUnitOfWork unitOfWork) : ILancamentoService
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

        var lancamento = Lancamento.CriacaoLancamento(conta, categoria, dto.valor, dto.data, dto.descricao, notificacao);

        conta.ValidarContaAtiva(notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        conta.AplicarLancamento(lancamento, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        var totalDespesasNoMes = await lancamentoRepository.SomarDespesasDaCategoriaNoMes(categoria.Id, lancamento.Data.Year, lancamento.Data.Month, cancellationToken);

        categoria.VerificarOrcamento(totalDespesasNoMes + lancamento.Valor, notificacao);

        await lancamentoRepository.AdicionarLancamento(lancamento, cancellationToken);
        await unitOfWork.Salvar(cancellationToken);

        return lancamento;
    }

    public async Task<Lancamento> ObterLancamentoPorId(Guid idLancamento, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var lancamento = await lancamentoRepository.ObterLancamentoPorId(idLancamento, cancellationToken);

        if (lancamento is null)
            notificacao.AdicionarNaoEncontrado("Lançamento não encontrado.");

        return lancamento;
    }

    public async Task<PaginacaoPrincipal<Lancamento>> ListarLancamentos(LancamentoListarDto dto, Notificacao notificacao, CancellationToken cancellationToken)
    {
        TipoCategoriaEnum? tipoLancamento = null;

        if (!string.IsNullOrWhiteSpace(dto.tipo))
        {
            if (Enum.TryParse(dto.tipo, true, out TipoCategoriaEnum tipo) && Enum.IsDefined(tipo))
                tipoLancamento = tipo;
            else
                notificacao.Adicionar("O tipo do lançamento passado é inválido.");
        }

        var filtro = FiltroLancamento.CriarFiltro(dto.contaId, dto.categoriaId, tipoLancamento, dto.dataInicio, dto.dataFim, dto.busca, notificacao);

        var paginacao = PaginacaoPrincipal<Lancamento>.CriarPaginacao(dto.pagina, dto.tamanhoPagina, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        await lancamentoRepository.ListarLancamentos(filtro, paginacao, cancellationToken);

        return paginacao;
    }

    public async Task<Lancamento> EditarLancamento(Guid idLancamento, LancamentoEditarDto dto, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var lancamento = await lancamentoRepository.ObterLancamentoPorId(idLancamento, cancellationToken);

        if (lancamento is null)
        {
            notificacao.AdicionarNaoEncontrado("Lançamento não encontrado.");
            return null;
        }

        var categoria = await categoriaRepository.ObterCategoriaPorId(dto.categoriaId, cancellationToken);

        if (categoria is null)
        {
            notificacao.AdicionarNaoEncontrado("Categoria não encontrada.");
            return null;
        }

        lancamento.Editar(categoria, dto.valor, dto.data, dto.descricao, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        await unitOfWork.Salvar(cancellationToken);

        return lancamento;
    }

    public async Task ExcluirLancamento(Guid idLancamento, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var lancamento = await lancamentoRepository.ObterLancamentoPorId(idLancamento, cancellationToken);

        if (lancamento is null)
        {
            notificacao.AdicionarNaoEncontrado("Lançamento não encontrado.");
            return;
        }

        lancamento.Conta.EstornarLancamento(lancamento, notificacao);

        if (notificacao.TemNotificacoes)
            return;

        await lancamentoRepository.RemoverLancamento(lancamento, cancellationToken);
        await unitOfWork.Salvar(cancellationToken);
    }
}
