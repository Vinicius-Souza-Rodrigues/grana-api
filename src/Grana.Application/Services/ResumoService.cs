using Application.Dtos;
using Application.Interfaces;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Services;

public sealed class ResumoService(ILancamentoRepository lancamentoRepository, IContaRepository contaRepository) : IResumoService
{
    public async Task<ResumoMensal> ObterResumoMensal(ResumoMensalDto dto, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var resumoMensal = ResumoMensal.CriarResumoMensal(dto.ano, dto.mes, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        var lancamentosDoMes = await lancamentoRepository.ListarLancamentosDoMes(resumoMensal.Ano, resumoMensal.Mes, cancellationToken);
        var lancamentosDoMesAnterior = await lancamentoRepository.ListarLancamentosDoMes(resumoMensal.AnoAnterior, resumoMensal.MesAnterior, cancellationToken);
        var contas = await contaRepository.ListarContas(false, cancellationToken);

        resumoMensal.Consolidar(lancamentosDoMes, lancamentosDoMesAnterior, contas);

        return resumoMensal;
    }
}
