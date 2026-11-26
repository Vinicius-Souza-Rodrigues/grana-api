using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Grana.Api.Controllers;
using Grana.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Tags("Resumo")]
[Route("api/resumo")]
public sealed class ResumoController(IResumoService resumoService) : ApiController
{
    [EndpointSummary("Mostra o resumo financeiro do mês")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpGet]
    public async Task<IActionResult> ObterResumoMensal([FromQuery] ResumoMensalDto resumoMensal, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var resumo = await resumoService.ObterResumoMensal(resumoMensal, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Resumo do mês encontrado.", MontarResumo(resumo));
    }

    private static object MontarResumo(ResumoMensal resumo)
    {
        if (resumo is null)
            return null;

        return new
        {
            ano = resumo.Ano,
            mes = resumo.Mes,
            totalReceitas = resumo.TotalReceitas,
            totalDespesas = resumo.TotalDespesas,
            resultadoDoMes = resumo.ResultadoDoMes,
            saldoAtualContas = resumo.SaldoAtualContas,
            comparativoMesAnterior = new Dictionary<string, decimal?>
            {
                ["receitas"] = resumo.ComparativoReceitas,
                ["despesas"] = resumo.ComparativoDespesas
            },
            despesasPorCategoria = resumo.DespesasPorCategoria.Select(MontarItem).ToList(),
            receitasPorCategoria = resumo.ReceitasPorCategoria.Select(MontarItem).ToList()
        };
    }

    private static object MontarItem(TotalPorCategoria totalPorCategoria)
    {
        return new
        {
            categoria = new { id = totalPorCategoria.Categoria.Id, nome = totalPorCategoria.Categoria.Nome },
            total = totalPorCategoria.Total,
            percentual = totalPorCategoria.Percentual
        };
    }
}
