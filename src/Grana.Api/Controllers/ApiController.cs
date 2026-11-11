using Domain.Enums;
using Grana.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;

namespace Grana.Api.Controllers;

[ApiController]
public abstract class ApiController : ControllerBase
{
    protected IActionResult Responder(Notificacao notificacao, int codigoSucesso, string mensagem, object dados = null)
    {
        if (notificacao.TemNotificacoes)
        {
            int codigoErro;
            mensagem = "Não foi possível concluir a operação.";
            
            switch (notificacao.Tipo)
            {
                case TipoNotificacaoEnum.NaoEncontrado:
                    codigoErro = 404;
                    break;
                case TipoNotificacaoEnum.Conflito:
                    codigoErro = 409;
                    break;
                default:
                    codigoErro = 400;
                    break;
            }

            return StatusCode(codigoErro, new { codigo = codigoErro, mensagem, erros = notificacao.Notificacoes });
        }   
        
        var avisos = notificacao.Avisos;

        if (!notificacao.TemAvisos)
            avisos = null;

        return StatusCode(codigoSucesso, new { codigo = codigoSucesso, mensagem, dados, avisos });
    }

    protected IActionResult ResponderCriado(Notificacao notificacao, string nomeAction, object rota, string mensagem, object dados)
    {
        if (notificacao.TemNotificacoes)
            return Responder(notificacao, 201, mensagem);

        var avisos = notificacao.Avisos;

        if (!notificacao.TemAvisos)
            avisos = null;

        return CreatedAtAction(nomeAction, rota, new { codigo = 201, mensagem, dados, avisos});
    }
}
