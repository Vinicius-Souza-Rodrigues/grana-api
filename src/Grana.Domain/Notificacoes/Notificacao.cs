namespace Grana.Domain.Notificacoes;

public sealed class Notificacao
{
    private readonly List<string> _notificacoes = [];

    public IReadOnlyList<string> Notificacoes => _notificacoes;
    public bool TemNotificacoes => _notificacoes.Count > 0;

    public void Adicionar(string notificacao) => _notificacoes.Add(notificacao);
}
