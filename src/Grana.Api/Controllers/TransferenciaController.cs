using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Grana.Api.Controllers;
using Grana.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/transferencias")]
public sealed class TransferenciaController(ITransferenciaService transferenciaService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> ListarTransferencias([FromQuery] TransferenciaListarDto transferenciaListar, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var transferencias = await transferenciaService.ListarTransferencias(transferenciaListar, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Transferências encontradas.", transferencias?.Select(MontarItem).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterTransferenciaPorId(Guid id, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var transferencia = await transferenciaService.ObterTransferenciaPorId(id, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Transferência encontrada.", MontarDetalhe(transferencia));
    }

    private static object MontarItem(Transferencia transferencia)
    {
        return new
        {
            id = transferencia.Id,
            data = transferencia.Data,
            valor = transferencia.Valor,
            descricao = transferencia.Descricao,
            contaOrigem = new { id = transferencia.ContaOrigem.Id, nome = transferencia.ContaOrigem.Nome },
            contaDestino = new { id = transferencia.ContaDestino.Id, nome = transferencia.ContaDestino.Nome }
        };
    }

    private static object MontarDetalhe(Transferencia transferencia)
    {
        if (transferencia is null)
            return null;

        return new
        {
            id = transferencia.Id,
            data = transferencia.Data,
            valor = transferencia.Valor,
            descricao = transferencia.Descricao,
            contaOrigem = new { id = transferencia.ContaOrigem.Id, nome = transferencia.ContaOrigem.Nome },
            contaDestino = new { id = transferencia.ContaDestino.Id, nome = transferencia.ContaDestino.Nome },
            criadoEm = transferencia.CriadoEm
        };
    }
}
