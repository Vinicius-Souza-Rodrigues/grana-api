using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Grana.Api.Controllers;
using Grana.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Tags("Categorias")]
[Route("api/categorias")]
public sealed class CategoriaController(ICategoriaService categoriaService) : ApiController
{
    [EndpointSummary("Cria uma categoria")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [HttpPost]
    public async Task<IActionResult> CriarCategoria(CategoriaCriarDto categoriaCriar, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var categoria = await categoriaService.CriarCategoria(categoriaCriar, notificacao, cancellationToken);

        return ResponderCriado(notificacao, nameof(ObterCategoriaPorId), new { id = categoria?.Id }, "Categoria criada com sucesso.", MontarDetalhe(categoria));
    }

    [EndpointSummary("Lista as categorias")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpGet]
    public async Task<IActionResult> ListarCategorias([FromQuery] string tipo, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var categorias = await categoriaService.ListarCategorias(tipo, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Categorias encontradas.", categorias?.Select(MontarDetalhe).ToList());
    }

    [EndpointSummary("Busca uma categoria pelo id")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObterCategoriaPorId(Guid id, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var categoria = await categoriaService.ObterCategoriaPorId(id, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Categoria encontrada.", MontarDetalhe(categoria));
    }

    [EndpointSummary("Edita o nome e o orçamento de uma categoria")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> EditarCategoria(Guid id, CategoriaEditarDto categoriaEditar, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var categoria = await categoriaService.EditarCategoria(id, categoriaEditar, notificacao, cancellationToken);

        return Responder(notificacao, 200, "Categoria atualizada com sucesso.", MontarDetalhe(categoria));
    }

    [EndpointSummary("Exclui uma categoria sem lançamentos")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> ExcluirCategoria(Guid id, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        await categoriaService.ExcluirCategoria(id, notificacao, cancellationToken);

        return ResponderSemConteudo(notificacao);
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
