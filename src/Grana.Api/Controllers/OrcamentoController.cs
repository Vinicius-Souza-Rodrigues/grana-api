using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Grana.Api.Controllers;
using Grana.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/orcamentos")]
public sealed class OrcamentoController(IOrcamentoService orcamentoService) : ApiController
{
    [HttpGet]
    public async Task<IActionResult> ListarOrcamentos([FromQuery] OrcamentoListarDto orcamentoListar, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var orcamentoMensal = await orcamentoService.ListarOrcamentos(orcamentoListar, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Orçamentos encontrados.", MontarOrcamentoMensal(orcamentoMensal));
    }

    private static object MontarOrcamentoMensal(OrcamentoMensal orcamentoMensal)
    {
        if (orcamentoMensal is null)
            return null;

        return new
        {
            ano = orcamentoMensal.Ano,
            mes = orcamentoMensal.Mes,
            totalOrcado = orcamentoMensal.TotalOrcado,
            totalGasto = orcamentoMensal.TotalGasto,
            categorias = orcamentoMensal.Categorias.Select(MontarItem).ToList()
        };
    }

    private static object MontarItem(OrcamentoCategoria orcamentoCategoria)
    {
        return new
        {
            categoria = new { id = orcamentoCategoria.Categoria.Id, nome = orcamentoCategoria.Categoria.Nome },
            limite = orcamentoCategoria.Limite,
            gasto = orcamentoCategoria.Gasto,
            disponivel = orcamentoCategoria.Disponivel,
            percentualUsado = orcamentoCategoria.PercentualUsado,
            situacao = orcamentoCategoria.Situacao
        };
    }
}
