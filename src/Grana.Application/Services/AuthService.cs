using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Services;

public sealed class AuthService(IUsuarioRepository usuarioRepository, IHashSenha hashSenha, IUnitOfWork unitOfWork) : IAuthService
{
    public async Task<Usuario> Registrar(UsuarioRegistrarDto dto, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var usuario = Usuario.CriacaoUsuario(dto.nome, dto.email, dto.senha, hashSenha.Gerar, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        if (await usuarioRepository.ExisteUsuarioComEmail(usuario.Email, cancellationToken))
        {
            notificacao.AdicionarConflito("Já existe um usuário com esse e-mail.");
            return null;
        }

        await usuarioRepository.AdicionarUsuario(usuario, cancellationToken);
        await unitOfWork.Salvar(notificacao, cancellationToken);

        if (notificacao.TemNotificacoes)
            return null;

        return usuario;
    }
}
