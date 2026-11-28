using Application.Interfaces;
using Domain.Models;
using Infraestructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Repositories;

public sealed class UsuarioRepository(GranaDbContext contexto) : IUsuarioRepository
{
    public async Task AdicionarUsuario(Usuario usuario, CancellationToken cancellationToken)
    {
        await contexto.Usuarios.AddAsync(usuario, cancellationToken);
    }

    public Task<bool> ExisteUsuarioComEmail(string email, CancellationToken cancellationToken)
    {
        var emailNormalizado = Usuario.NormalizarEmail(email);

        return contexto.Usuarios.AnyAsync(u => u.Email == emailNormalizado, cancellationToken);
    }

    public Task<Usuario> ObterUsuarioPorEmail(string email, CancellationToken cancellationToken)
    {
        var emailNormalizado = Usuario.NormalizarEmail(email);

        return contexto.Usuarios.FirstOrDefaultAsync(u => u.Email == emailNormalizado, cancellationToken);
    }
}
