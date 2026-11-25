using Domain.Enums;
using Grana.Domain.Notificacoes;

namespace Domain.Models;

public sealed class Transferencia
{
    private Transferencia(Conta contaOrigem, Conta contaDestino, decimal valor, DateOnly data, string descricao)
    {
        Id = Guid.NewGuid();
        IdContaOrigem = contaOrigem.Id;
        ContaOrigem = contaOrigem;
        IdContaDestino = contaDestino.Id;
        ContaDestino = contaDestino;
        Valor = valor;
        Data = data;
        Descricao = descricao;
        CriadoEm = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid IdContaOrigem { get; private set; }
    public Conta ContaOrigem { get; private set; }
    public Guid IdContaDestino { get; private set; }
    public Conta ContaDestino { get; private set; }
    public decimal Valor { get; private set; }
    public DateOnly Data { get; private set; }
    public string Descricao { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public const int TamanhoMaximoDescricao = 100;

    public static Transferencia CriacaoTransferencia(Conta contaOrigem, Conta contaDestino, decimal valor, DateOnly data, string descricao, Notificacao notificacao)
    {
        descricao = descricao?.Trim();

        if (string.IsNullOrEmpty(descricao))
            descricao = null;

        if (contaOrigem is null)
            notificacao.Adicionar("A conta de origem é obrigatória.");

        if (contaDestino is null)
            notificacao.Adicionar("A conta de destino é obrigatória.");

        if (contaOrigem is not null && contaDestino is not null && contaOrigem.Id == contaDestino.Id)
            notificacao.Adicionar("A conta de origem e a de destino devem ser diferentes.");

        if (contaOrigem is not null && contaOrigem.StatusAtividadeConta == StatusAtividadeContaEnum.Inativo)
            notificacao.Adicionar("A conta de origem está desativada.");

        if (contaDestino is not null && contaDestino.StatusAtividadeConta == StatusAtividadeContaEnum.Inativo)
            notificacao.Adicionar("A conta de destino está desativada.");

        if (valor <= 0)
            notificacao.Adicionar("O valor deve ser maior que zero.");

        if (valor != decimal.Round(valor, 2))
            notificacao.Adicionar("O valor possui mais que duas casas decimais.");

        if (contaOrigem is not null && valor > 0 && contaOrigem.Saldo < valor)
            notificacao.Adicionar("Saldo insuficiente.");

        if (data > DateOnly.FromDateTime(DateTime.UtcNow))
            notificacao.Adicionar("A data da transferência não pode ser futura.");

        if (descricao is not null && descricao.Length > TamanhoMaximoDescricao)
            notificacao.Adicionar("A descrição não pode ter mais que 100 caracteres.");

        if (notificacao.TemNotificacoes)
            return null;

        return new Transferencia(contaOrigem, contaDestino, valor, data, descricao);
    }

    public void Efetivar(Notificacao notificacao)
    {
        ContaDestino.ValidarContaAtiva(notificacao);

        if (notificacao.TemNotificacoes)
            return;

        ContaOrigem.Debitar(Valor, notificacao);

        if (notificacao.TemNotificacoes)
            return;

        ContaDestino.Creditar(Valor, notificacao);
    }

    public void Estornar(Notificacao notificacao)
    {
        if (ContaOrigem.StatusAtividadeConta == StatusAtividadeContaEnum.Inativo)
            notificacao.Adicionar("A conta de origem está desativada.");

        if (ContaDestino.StatusAtividadeConta == StatusAtividadeContaEnum.Inativo)
            notificacao.Adicionar("A conta de destino está desativada.");

        if (ContaDestino.Saldo < Valor)
            notificacao.Adicionar("Saldo insuficiente.");

        if (notificacao.TemNotificacoes)
            return;

        ContaDestino.Debitar(Valor, notificacao);

        if (notificacao.TemNotificacoes)
            return;

        ContaOrigem.Creditar(Valor, notificacao);
    }
}
