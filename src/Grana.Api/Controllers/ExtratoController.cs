using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Grana.Api.Controllers;
using Grana.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/contas/{id:guid}/extrato")]
public sealed class ExtratoController(IExtratoService extratoService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> ObterExtratoConta(Guid id, [FromQuery] ExtratoContaDto extratoConta, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var extrato = await extratoService.ObterExtratoConta(id, extratoConta, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Extrato encontrado.", MontarExtrato(extrato));
    }

    private static object MontarExtrato(ExtratoConta extrato)
    {
        if (extrato is null)
            return null;

        return new
        {
            conta = new { id = extrato.Conta.Id, nome = extrato.Conta.Nome },
            dataInicio = extrato.DataInicio,
            dataFim = extrato.DataFim,
            saldoAnterior = extrato.SaldoAnterior,
            totalEntradas = extrato.TotalEntradas,
            totalSaidas = extrato.TotalSaidas,
            saldoFinal = extrato.SaldoFinal,
            movimentacoes = extrato.Movimentacoes.Select(MontarMovimentacao).ToList()
        };
    }

    private static object MontarMovimentacao(MovimentacaoExtrato movimentacao)
    {
        return new
        {
            id = movimentacao.Id,
            tipo = movimentacao.Tipo,
            data = movimentacao.Data,
            descricao = movimentacao.Descricao,
            valor = movimentacao.ValorComSinal,
            saldoApos = movimentacao.SaldoApos
        };
    }
}
