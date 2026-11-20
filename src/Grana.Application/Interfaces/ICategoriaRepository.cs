using Domain.Enums;
using Domain.Models;

namespace Application.Interfaces;

public interface ICategoriaRepository
{
    Task AdicionarCategoria(Categoria categoria, CancellationToken cancellationToken);

    Task<Categoria> ObterCategoriaPorId(Guid idCategoria, CancellationToken cancellationToken);

    Task<bool> ExisteCategoriaComNome(string nome, TipoCategoriaEnum tipoCategoria, Guid idCategoriaIgnorada, CancellationToken cancellationToken);

    Task<IReadOnlyList<Categoria>> ListarCategorias(TipoCategoriaEnum? tipoCategoria, CancellationToken cancellationToken);

    Task RemoverCategoria(Categoria categoria, CancellationToken cancellationToken);
}
