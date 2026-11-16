using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Grana.Api.Controllers;
using Grana.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/categorias")]
public sealed class CategoriaController(ICategoriaService categoriaService) : ApiController
{
    [HttpPost]
    public async Task<IActionResult> CriarCategoria(CategoriaCriarDto categoriaCriar, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var categoria = await categoriaService.CriarCategoria(categoriaCriar, notificacao, cancellationToken);

        return ResponderCriado(notificacao, nameof(ObterCategoriaPorId), new { id = categoria?.Id }, "Categoria criada com sucesso.", MontarDetalhe(categoria));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterCategoriaPorId(Guid id, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var categoria = await categoriaService.ObterCategoriaPorId(id, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Categoria encontrada.", MontarDetalhe(categoria));
    }

    private static object MontarDetalhe(Categoria categoria)
    {
        if (categoria is null)
            return null;

        return new
        {
            id = categoria.Id,
            nome = categoria.Nome,
            tipo = categoria.TipoCategoria,
            orcamentoMensal = categoria.OrcamentoMensal
        };
    }
}
