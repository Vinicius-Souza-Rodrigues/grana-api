using Application.Dtos;
using Application.Interfaces;
using Grana.Api.Controllers;
using Grana.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/contas")]
public sealed class ContaController(IContaService contaService) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> CriarConta(ContaCriarDto contaCriar, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var conta = await contaService.CriarConta(contaCriar, notificacao, cancellationToken);

        return ResponderCriado(notificacao, nameof(ObterContaPorId), new { id = conta?.Id }, "Conta criada com sucesso.", conta);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterContaPorId(Guid id, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var conta = await contaService.ObterContaPorId(id, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Conta encontrada.", conta);
    }
}

