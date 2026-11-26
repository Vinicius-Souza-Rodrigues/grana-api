using Application.Interfaces;
using Domain.Enums;
using Domain.Models;
using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories;

public sealed class CategoriaRepository(GranaDbContext contexto) : ICategoriaRepository
{
    public async Task AdicionarCategoria(Categoria categoria, CancellationToken cancellationToken)
    {
        await contexto.Categorias.AddAsync(categoria, cancellationToken);
    }

    public Task<Categoria> ObterCategoriaPorId(Guid idCategoria, CancellationToken cancellationToken)
    {
        return contexto.Categorias.FirstOrDefaultAsync(c => c.Id == idCategoria, cancellationToken);
    }

    public Task<bool> ExisteCategoriaComNome(string nome, TipoCategoriaEnum tipoCategoria, Guid idCategoriaIgnorada, CancellationToken cancellationToken)
    {
        var nomeNormalizado = nome?.ToLower();

        return contexto.Categorias.AnyAsync(c =>
            c.Id != idCategoriaIgnorada &&
            c.TipoCategoria == tipoCategoria &&
            c.Nome.ToLower() == nomeNormalizado,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Categoria>> ListarCategorias(TipoCategoriaEnum? tipoCategoria, CancellationToken cancellationToken)
    {
        return await contexto.Categorias
            .Where(c => tipoCategoria == null || c.TipoCategoria == tipoCategoria)
            .OrderBy(c => c.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task RemoverCategoria(Categoria categoria, CancellationToken cancellationToken)
    {
        contexto.Categorias.Remove(categoria);

        return Task.CompletedTask;
    }
}
