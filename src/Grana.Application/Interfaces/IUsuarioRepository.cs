using Domain.Models;

namespace Application.Interfaces;

public interface IUsuarioRepository
{
    Task AdicionarUsuario(Usuario usuario, CancellationToken cancellationToken);

    Task<bool> ExisteUsuarioComEmail(string email, CancellationToken cancellationToken);

    Task<Usuario> ObterUsuarioPorEmail(string email, CancellationToken cancellationToken);
}
