using Domain.Enums;

namespace Grana.Domain.Notificacoes;

public sealed class Notificacao
{
    private readonly List<string> _notificacoes = [];
    private readonly List<string> _avisos = [];

    public IReadOnlyList<string> Notificacoes => _notificacoes;
    public IReadOnlyList<string> Avisos => _avisos;
    public TipoNotificacaoEnum Tipo { get; private set; } = TipoNotificacaoEnum.Validacao;

    public bool TemNotificacoes => _notificacoes.Count > 0;

    public bool TemAvisos => _avisos.Count > 0;

    public void Adicionar(string notificacao) => _notificacoes.Add(notificacao);

    public void AdicionarNaoEncontrado(string notificacao)
    {
        Tipo = TipoNotificacaoEnum.NaoEncontrado;
        _notificacoes.Add(notificacao);
    }

    public void AdicionarConflito(string notificacao)
    {
        Tipo = TipoNotificacaoEnum.Conflito;
        _notificacoes.Add(notificacao);
    }

    public void AdicionarAviso(string aviso) => _avisos.Add(aviso);
}
