using Application.Dtos;
using Application.Interfaces;
using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Services;

public sealed class OrcamentoService(ICategoriaRepository categoriaRepository, ILancamentoRepository lancamentoRepository) : IOrcamentoService
{
    public async Task<OrcamentoMensal> ListarOrcamentos(OrcamentoListarDto dto, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var orcamentoMensal = OrcamentoMensal.CriarOrcamentoMensal(dto.ano, dto.mes, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        var categorias = await categoriaRepository.ListarCategorias(TipoCategoriaEnum.Despesa, cancellationToken);

        foreach (var categoria in categorias)
        {
            var gasto = await lancamentoRepository.SomarDespesasDaCategoriaNoMes(categoria.Id, orcamentoMensal.Ano, orcamentoMensal.Mes, cancellationToken);

            orcamentoMensal.AdicionarCategoria(categoria, gasto);
        }

        return orcamentoMensal;
    }
}
