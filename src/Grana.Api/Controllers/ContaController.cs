using Application.Dtos;
using Application.Interfaces;
using Domain.Enums;
using Domain.Models;
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

        return ResponderCriado(notificacao, nameof(ObterContaPorId), new { id = conta?.Id }, "Conta criada com sucesso.", MontarDetalhe(conta));
    }

    [HttpGet]
    public async Task<IActionResult> ListarContas([FromQuery] bool incluirInativas, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var contas = await contaService.ListarContas(incluirInativas, cancellationToken);

        return Responder(notificacao, 200, "Contas encontradas.", contas.Select(MontarItem).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterContaPorId(Guid id, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var conta = await contaService.ObterContaPorId(id, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Conta encontrada.", MontarDetalhe(conta));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> RenomearConta(Guid id, ContaRenomearDto contaRenomear, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var conta = await contaService.RenomearConta(id, contaRenomear, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Conta atualizada com sucesso.", MontarDetalhe(conta));
    }

    [HttpPatch("{id:guid}/desativar")]
    public async Task<IActionResult> DesativarConta(Guid id, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var conta = await contaService.DesativarConta(id, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Conta desativada com sucesso.", MontarDetalhe(conta));
    }

    [HttpPatch("{id:guid}/reativar")]
    public async Task<IActionResult> ReativarConta(Guid id, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var conta = await contaService.ReativarConta(id, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Conta reativada com sucesso.", MontarDetalhe(conta));
    }

    private static object MontarItem(Conta conta)
    {
        return new
        {
            id = conta.Id,
            nome = conta.Nome,
            tipo = conta.TipoConta,
            saldo = conta.Saldo,
            ativa = conta.StatusAtividadeConta == StatusAtividadeContaEnum.Ativo
        };
    }

    private static object MontarDetalhe(Conta conta)
    {
        if (conta is null)
            return null;

        return new
        {
            id = conta.Id,
            nome = conta.Nome,
            tipo = conta.TipoConta,
            saldo = conta.Saldo,
            ativa = conta.StatusAtividadeConta == StatusAtividadeContaEnum.Ativo,
            saldoInicial = conta.SaldoInicial,
            criadoEm = conta.CriadoEm
        };
    }
}
