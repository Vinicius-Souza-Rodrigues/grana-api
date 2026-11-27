using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Grana.Api.Controllers;
using Grana.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Tags("Autenticação")]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ApiController
{
    [EndpointSummary("Registra um usuário")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar(UsuarioRegistrarDto usuarioRegistrar, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var usuario = await authService.Registrar(usuarioRegistrar, notificacao, cancellationToken);

        return Responder(notificacao, 201, "Usuário registrado com sucesso.", MontarDetalhe(usuario));
    }

    private static object MontarDetalhe(Usuario usuario)
    {
        if (usuario is null)
            return null;

        return new
        {
            id = usuario.Id,
            nome = usuario.Nome,
            email = usuario.Email,
            criadoEm = usuario.CriadoEm
        };
    }
}
