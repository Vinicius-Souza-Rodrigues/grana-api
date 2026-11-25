using Application.Interfaces;
using Domain.Enums;
using Domain.Models;
using Domain.Shared.Paginacao;

namespace Infraestructure.Repositories;

public sealed class LancamentoRepository : ILancamentoRepository
{
    private List<Lancamento> _lancamentos = [];

    public Task AdicionarLancamento(Lancamento lancamento, CancellationToken cancellationToken)
    {
        _lancamentos.Add(lancamento);

        return Task.CompletedTask;
    }

    public Task<Lancamento> ObterLancamentoPorId(Guid idLancamento, CancellationToken cancellationToken)
    {
        var lancamento = _lancamentos.Find(l => l.Id.Equals(idLancamento));

        return Task.FromResult(lancamento);
    }

    public Task RemoverLancamento(Lancamento lancamento, CancellationToken cancellationToken)
    {
        _lancamentos.Remove(lancamento);

        return Task.CompletedTask;
    }

    public Task ListarLancamentos(FiltroLancamento filtro, PaginacaoPrincipal<Lancamento> paginacao, CancellationToken cancellationToken)
    {
        var filtrados = _lancamentos
            .Where(l =>
                (filtro.IdConta is null || l.IdConta == filtro.IdConta) &&
                (filtro.IdCategoria is null || l.IdCategoria == filtro.IdCategoria) &&
                (filtro.TipoLancamento is null || l.TipoLancamento == filtro.TipoLancamento) &&
                (filtro.DataInicio is null || l.Data >= filtro.DataInicio) &&
                (filtro.DataFim is null || l.Data <= filtro.DataFim) &&
                (filtro.Busca is null || (l.Descricao is not null && l.Descricao.Contains(filtro.Busca, StringComparison.OrdinalIgnoreCase))))
            .ToList();

        IReadOnlyList<Lancamento> itens = filtrados
            .OrderByDescending(l => l.Data)
            .ThenByDescending(l => l.CriadoEm)
            .Skip((paginacao.Pagina - 1) * paginacao.TamanhoPagina)
            .Take(paginacao.TamanhoPagina)
            .ToList();

        paginacao.PreencherPaginacao(itens, filtrados.Count);

        return Task.CompletedTask;
    }

    public Task<bool> ExisteLancamentoComCategoria(Guid idCategoria, CancellationToken cancellationToken)
    {
        var existe = _lancamentos.Exists(l => l.IdCategoria == idCategoria);

        return Task.FromResult(existe);
    }

    public Task<IReadOnlyList<Lancamento>> ListarLancamentosDoMes(int ano, int mes, CancellationToken cancellationToken)
    {
        IReadOnlyList<Lancamento> lancamentos = _lancamentos
            .Where(l => l.Data.Year == ano && l.Data.Month == mes)
            .ToList();

        return Task.FromResult(lancamentos);
    }

    public Task<decimal> SomarDespesasDaCategoriaNoMes(Guid idCategoria, int ano, int mes, CancellationToken cancellationToken)
    {
        var total = _lancamentos
            .Where(l =>
                l.IdCategoria == idCategoria &&
                l.TipoLancamento == TipoCategoriaEnum.Despesa &&
                l.Data.Year == ano &&
                l.Data.Month == mes)
            .Sum(l => l.Valor);

        return Task.FromResult(total);
    }
}
