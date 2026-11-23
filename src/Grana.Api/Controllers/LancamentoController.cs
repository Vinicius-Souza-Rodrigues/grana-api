using Application.Interfaces;
using Domain.Models;
using Grana.Api.Controllers;
using Grana.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/lancamentos")]
public sealed class LancamentoController(ILancamentoService lancamentoService) : ApiController
{
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
