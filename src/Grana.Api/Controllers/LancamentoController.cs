using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Domain.Shared.Paginacao;
using Grana.Api.Controllers;
using Grana.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/lancamentos")]
public sealed class LancamentoController(ILancamentoService lancamentoService) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> CriarLancamento(LancamentoCriarDto lancamentoCriar, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var lancamento = await lancamentoService.CriarLancamento(lancamentoCriar, notificacao, cancellationToken);

        return ResponderCriado(notificacao, nameof(ObterLancamentoPorId), new { id = lancamento?.Id }, "Lançamento criado com sucesso.", MontarCriado(lancamento));
    }

    [HttpGet]
    public async Task<IActionResult> ListarLancamentos([FromQuery] LancamentoListarDto lancamentoListar, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var paginacao = await lancamentoService.ListarLancamentos(lancamentoListar, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Lançamentos encontrados.", MontarPaginacao(paginacao));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterLancamentoPorId(Guid id, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var lancamento = await lancamentoService.ObterLancamentoPorId(id, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Lançamento encontrado.", MontarDetalhe(lancamento));
    }

    private static object MontarCriado(Lancamento lancamento)
    {
        if (lancamento is null)
            return null;

        return new
        {
            lancamento = MontarDetalhe(lancamento),
            saldoConta = lancamento.Conta.Saldo
        };
    }

    private static object MontarPaginacao(PaginacaoPrincipal<Lancamento> paginacao)
    {
        if (paginacao is null)
            return null;

        return new
        {
            itens = paginacao.Itens.Select(MontarItem).ToList(),
            pagina = paginacao.Pagina,
            tamanhoPagina = paginacao.TamanhoPagina,
            totalItens = paginacao.TotalItens,
            totalPaginas = paginacao.TotalPaginas
        };
    }

    private static object MontarItem(Lancamento lancamento)
    {
        return new
        {
            id = lancamento.Id,
            data = lancamento.Data,
            descricao = lancamento.Descricao,
            valor = lancamento.Valor,
            tipo = lancamento.TipoLancamento,
            conta = new { id = lancamento.Conta.Id, nome = lancamento.Conta.Nome },
            categoria = new { id = lancamento.Categoria.Id, nome = lancamento.Categoria.Nome }
        };
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
