using Application.Dtos;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<Usuario> Registrar(UsuarioRegistrarDto dto, Notificacao notificacao, CancellationToken cancellationToken);
}
