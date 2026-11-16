using Application.Interfaces;
using Domain.Enums;
using Domain.Models;

namespace Infraestructure.Repositories;

public sealed class CategoriaRepository : ICategoriaRepository
{
    private List<Categoria> _categorias = [];

    public Task AdicionarCategoria(Categoria categoria, CancellationToken cancellationToken)
    {
        _categorias.Add(categoria);

        return Task.CompletedTask;
    }

    public Task<Categoria> ObterCategoriaPorId(Guid idCategoria, CancellationToken cancellationToken)
    {
        var categoria = _categorias.Find(c => c.Id.Equals(idCategoria));

        return Task.FromResult(categoria);
    }

    public Task<bool> ExisteCategoriaComNome(string nome, TipoCategoriaEnum tipoCategoria, Guid idCategoriaIgnorada, CancellationToken cancellationToken)
    {
        var existe = _categorias.Exists(c =>
            c.Id != idCategoriaIgnorada &&
            c.TipoCategoria == tipoCategoria &&
            string.Equals(c.Nome, nome, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(existe);
    }
}
