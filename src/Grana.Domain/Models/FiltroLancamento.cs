using Domain.Enums;
using Grana.Domain.Notificacoes;

namespace Domain.Models;

public sealed class FiltroLancamento
{
    private FiltroLancamento(Guid? idConta, Guid? idCategoria, TipoCategoriaEnum? tipoLancamento, DateOnly? dataInicio, DateOnly? dataFim, string busca)
    {
        IdConta = idConta;
        IdCategoria = idCategoria;
        TipoLancamento = tipoLancamento;
        DataInicio = dataInicio;
        DataFim = dataFim;
        Busca = busca;
    }

    public Guid? IdConta { get; private set; }
    public Guid? IdCategoria { get; private set; }
    public TipoCategoriaEnum? TipoLancamento { get; private set; }
    public DateOnly? DataInicio { get; private set; }
    public DateOnly? DataFim { get; private set; }
    public string Busca { get; private set; }

    public static FiltroLancamento CriarFiltro(Guid? idConta, Guid? idCategoria, TipoCategoriaEnum? tipoLancamento, DateOnly? dataInicio, DateOnly? dataFim, string busca, Notificacao notificacao)
    {
        busca = busca?.Trim();

        if (string.IsNullOrEmpty(busca))
            busca = null;

        if (dataInicio > dataFim)
            notificacao.Adicionar("A data de início não pode ser maior que a data de fim.");

        if (notificacao.TemNotificacoes)
            return null;

        return new FiltroLancamento(idConta, idCategoria, tipoLancamento, dataInicio, dataFim, busca);
    }
}
