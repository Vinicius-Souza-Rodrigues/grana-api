using Application.Interfaces;
using Domain.Enums;
using Domain.Models;
using Domain.Shared.Paginacao;
using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories;

public sealed class LancamentoRepository(GranaDbContext contexto) : ILancamentoRepository
{
    private IQueryable<Lancamento> LancamentosCompletos => contexto.Lancamentos
        .Include(l => l.Conta)
        .Include(l => l.Categoria);

    public async Task AdicionarLancamento(Lancamento lancamento, CancellationToken cancellationToken)
    {
        await contexto.Lancamentos.AddAsync(lancamento, cancellationToken);
    }

    public Task<Lancamento> ObterLancamentoPorId(Guid idLancamento, CancellationToken cancellationToken)
    {
        return LancamentosCompletos.FirstOrDefaultAsync(l => l.Id == idLancamento, cancellationToken);
    }

    public Task RemoverLancamento(Lancamento lancamento, CancellationToken cancellationToken)
    {
        contexto.Lancamentos.Remove(lancamento);

        return Task.CompletedTask;
    }

    public async Task ListarLancamentos(FiltroLancamento filtro, PaginacaoPrincipal<Lancamento> paginacao, CancellationToken cancellationToken)
    {
        var busca = filtro.Busca?.ToLower();

        var filtrados = LancamentosCompletos
            .Where(l =>
                (filtro.IdConta == null || l.IdConta == filtro.IdConta) &&
                (filtro.IdCategoria == null || l.IdCategoria == filtro.IdCategoria) &&
                (filtro.TipoLancamento == null || l.TipoLancamento == filtro.TipoLancamento) &&
                (filtro.DataInicio == null || l.Data >= filtro.DataInicio) &&
                (filtro.DataFim == null || l.Data <= filtro.DataFim) &&
                (busca == null || (l.Descricao != null && l.Descricao.ToLower().Contains(busca))));

        var totalItens = await filtrados.CountAsync(cancellationToken);

        var itens = await filtrados
            .OrderByDescending(l => l.Data)
            .ThenByDescending(l => l.CriadoEm)
            .Skip((paginacao.Pagina - 1) * paginacao.TamanhoPagina)
            .Take(paginacao.TamanhoPagina)
            .ToListAsync(cancellationToken);

        paginacao.PreencherPaginacao(itens, totalItens);
    }

    public Task<bool> ExisteLancamentoComCategoria(Guid idCategoria, CancellationToken cancellationToken)
    {
        return contexto.Lancamentos.AnyAsync(l => l.IdCategoria == idCategoria, cancellationToken);
    }

    public async Task<IReadOnlyList<Lancamento>> ListarLancamentosDoMes(int ano, int mes, CancellationToken cancellationToken)
    {
        var inicio = new DateOnly(ano, mes, 1);
        var fim = inicio.AddMonths(1);

        return await LancamentosCompletos
            .Where(l => l.Data >= inicio && l.Data < fim)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Lancamento>> ListarLancamentosDaContaAte(Guid idConta, DateOnly dataFim, CancellationToken cancellationToken)
    {
        return await contexto.Lancamentos
            .Where(l => l.IdConta == idConta && l.Data <= dataFim)
            .ToListAsync(cancellationToken);
    }

    public Task<decimal> SomarDespesasDaCategoriaNoMes(Guid idCategoria, int ano, int mes, CancellationToken cancellationToken)
    {
        var inicio = new DateOnly(ano, mes, 1);
        var fim = inicio.AddMonths(1);

        return contexto.Lancamentos
            .Where(l =>
                l.IdCategoria == idCategoria &&
                l.TipoLancamento == TipoCategoriaEnum.Despesa &&
                l.Data >= inicio &&
                l.Data < fim)
            .SumAsync(l => l.Valor, cancellationToken);
    }
}
