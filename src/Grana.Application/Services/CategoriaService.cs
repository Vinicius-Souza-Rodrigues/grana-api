using Application.Dtos;
using Application.Interfaces;
using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Services;

public sealed class CategoriaService(ICategoriaRepository categoriaRepository) : ICategoriaService
{
    public async Task<Categoria> CriarCategoria(CategoriaCriarDto dto, Notificacao notificacao, CancellationToken cancellationToken)
    {
        Enum.TryParse(dto.tipo, true, out TipoCategoriaEnum tipoCategoria);

        var categoria = Categoria.CriacaoCategoria(dto.nome, tipoCategoria, dto.orcamentoMensal, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        if (await categoriaRepository.ExisteCategoriaComNome(categoria.Nome, categoria.TipoCategoria, categoria.Id, cancellationToken))
        {
            notificacao.AdicionarConflito("Já existe uma categoria com esse nome para esse tipo.");
            return null;
        }

        await categoriaRepository.AdicionarCategoria(categoria, cancellationToken);

        return categoria;
    }

    public async Task<Categoria> ObterCategoriaPorId(Guid idCategoria, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var categoria = await categoriaRepository.ObterCategoriaPorId(idCategoria, cancellationToken);

        if (categoria is null)
            notificacao.AdicionarNaoEncontrado("Categoria não encontrada.");

        return categoria;
    }

    public async Task<IReadOnlyList<Categoria>> ListarCategorias(string tipo, Notificacao notificacao, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tipo))
            return await categoriaRepository.ListarCategorias(null, cancellationToken);

        if (!Enum.TryParse(tipo, true, out TipoCategoriaEnum tipoCategoria) || !Enum.IsDefined(tipoCategoria))
        {
            notificacao.Adicionar("O tipo da categoria passado é inválido.");
            return null;
        }

        return await categoriaRepository.ListarCategorias(tipoCategoria, cancellationToken);
    }
}
