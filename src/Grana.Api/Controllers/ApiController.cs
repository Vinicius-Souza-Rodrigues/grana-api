using Grana.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;

namespace Grana.Api.Controllers;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected IActionResult Responder(Notificacao notificacao, int codigoSucesso, string mensagem)
    {
        if (notificacao.TemNotificacoes)
            return BadRequest(new { codigo = 400, mensagem = "Não foi possível concluir a operação.", erros = notificacao.Notificacoes });

        return StatusCode(codigoSucesso, new { codigo = codigoSucesso, mensagem });
    }
}
