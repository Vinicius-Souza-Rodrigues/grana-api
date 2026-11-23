using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Grana.Api.Controllers;
using Grana.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/lancamentos")]
public sealed class LancamentoController(ILancamentoService lancamentoService, IContaService contaService) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> CriarLancamento(LancamentoCriarDto lancamentoCriar, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var lancamento = await lancamentoService.CriarLancamento(lancamentoCriar, notificacao, cancellationToken);

        if (notificacao.TemNotificacoes)
            return Responder(notificacao, 201, null);

        var conta = await contaService.ObterContaPorId(lancamento.IdConta, notificacao, cancellationToken);

        return ResponderCriado(notificacao, nameof(ObterLancamentoPorId), new { id = lancamento.Id }, "Lançamento criado com sucesso.", new { lancamento = MontarDetalhe(lancamento), saldoConta = conta.Saldo });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterLancamentoPorId(Guid id, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var lancamento = await lancamentoService.ObterLancamentoPorId(id, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Lançamento encontrado.", MontarDetalhe(lancamento));
    }

    private static object MontarDetalhe(Lancamento lancamento)
    {
        if (lancamento is null)
            return null;

        return new
        {
            id = lancamento.Id,
            contaId = lancamento.IdConta,
            categoriaId = lancamento.IdCategoria,
            tipo = lancamento.TipoLancamento,
            valor = lancamento.Valor,
            data = lancamento.Data,
            descricao = lancamento.Descricao,
            criadoEm = lancamento.CriadoEm
        };
    }
}
