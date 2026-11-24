using Grana.Domain.Notificacoes;

namespace Domain.Models;

public sealed class FiltroTransferencia
{
    private FiltroTransferencia(Guid? idConta, DateOnly? dataInicio, DateOnly? dataFim)
    {
        IdConta = idConta;
        DataInicio = dataInicio;
        DataFim = dataFim;
    }

    public Guid? IdConta { get; private set; }
    public DateOnly? DataInicio { get; private set; }
    public DateOnly? DataFim { get; private set; }

    public static FiltroTransferencia CriarFiltro(Guid? idConta, DateOnly? dataInicio, DateOnly? dataFim, Notificacao notificacao)
    {
        if (dataInicio > dataFim)
            notificacao.Adicionar("A data de início não pode ser maior que a data de fim.");

        if (notificacao.TemNotificacoes)
            return null;

        return new FiltroTransferencia(idConta, dataInicio, dataFim);
    }
}
