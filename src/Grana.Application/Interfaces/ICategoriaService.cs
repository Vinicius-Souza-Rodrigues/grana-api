using Application.Dtos;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Interfaces;

public interface ICategoriaService
{
    Task<Categoria> CriarCategoria(CategoriaCriarDto dto, Notificacao notificacao, CancellationToken cancellationToken);

    Task<Categoria> ObterCategoriaPorId(Guid idCategoria, Notificacao notificacao, CancellationToken cancellationToken);

    Task<IReadOnlyList<Categoria>> ListarCategorias(string tipo, Notificacao notificacao, CancellationToken cancellationToken);

    Task<Categoria> EditarCategoria(Guid idCategoria, CategoriaEditarDto dto, Notificacao notificacao, CancellationToken cancellationToken);

    Task ExcluirCategoria(Guid idCategoria, Notificacao notificacao, CancellationToken cancellationToken);
}
