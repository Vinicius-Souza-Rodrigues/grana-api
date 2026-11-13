using Domain.Enums;
using Grana.Domain.Notificacoes;

namespace Domain.Models;

public sealed class Conta
{
    private Conta(string nome, TipoContaEnum tipoConta, decimal saldo)
    {
        Id = Guid.NewGuid();
        Nome = nome;
        TipoConta = tipoConta;
        Saldo = saldo;
        SaldoInicial = saldo;
        StatusAtividadeConta = StatusAtividadeContaEnum.Ativo;
        CriadoEm = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public TipoContaEnum TipoConta { get; private set; }
    public decimal Saldo { get; private set; }
    public decimal SaldoInicial { get; private set; }
    public StatusAtividadeContaEnum StatusAtividadeConta { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public const int TamanhoMaximoNome = 60;
    public const int TamanhoMinimoNome = 3;
    public const decimal SaldoMinimo = 0;

    public static Conta CriacaoConta(string nome, TipoContaEnum tipoConta, decimal saldo, Notificacao notificacao)
    {
        nome = nome?.Trim();

        if (string.IsNullOrWhiteSpace(nome))
            notificacao.Adicionar("O nome passado é invalido, não pode ser nulo");
        
        else if (nome.Length < TamanhoMinimoNome || nome.Length > TamanhoMaximoNome)
            notificacao.Adicionar("o nome nao pode ter menos que 3 caracteres ou mais que 60");

        if (saldo < SaldoMinimo)
            notificacao.Adicionar("O saldo inicial não pode ser menor que 0 ou nulo");

        if (!Enum.IsDefined(tipoConta))
            notificacao.Adicionar("O tipo conta passado é invalido");

        if (saldo != decimal.Round(saldo, 2))
            notificacao.Adicionar("O saldo possui mais que duas casas decimais");

        if (notificacao.TemNotificacoes)
            return null;

        return new Conta(nome, tipoConta, saldo);
    }

    public void RenomearNome(string nome, Notificacao notificacao)
    {
        nome = nome?.Trim();

        if (string.IsNullOrWhiteSpace(nome))
            notificacao.Adicionar("O nome passado é invalido, não pode ser nulo");

        else if (nome.Length < TamanhoMinimoNome || nome.Length > TamanhoMaximoNome)
            notificacao.Adicionar("o nome nao pode ter menos que 3 caracteres ou mais que 60");

        if (StatusAtividadeConta == StatusAtividadeContaEnum.Inativo)
            notificacao.Adicionar("Não é possível editar uma conta desativada.");

        if (notificacao.TemNotificacoes)
            return;

        Nome = nome;
    }

    public void Creditar(decimal valorCredito, Notificacao notificacao)
    {
        if (valorCredito <= 0)
            notificacao.Adicionar("O valor deve ser maior que zero");

        if (StatusAtividadeConta == StatusAtividadeContaEnum.Inativo)
            notificacao.Adicionar("O status de atividade da conta não pode ser inativo");

         if (valorCredito != decimal.Round(valorCredito, 2))
            notificacao.Adicionar("O saldo possui mais que duas casas decimais");

        if (notificacao.TemNotificacoes)
            return;

        Saldo += valorCredito;
    }

    public void Debitar(decimal valorDebito, Notificacao notificacao)
    {
        decimal SaldoTemporario = Saldo - valorDebito;

        if (valorDebito <= 0)
            notificacao.Adicionar("O valor deve ser maior que zero");

        if (StatusAtividadeConta == StatusAtividadeContaEnum.Inativo)
            notificacao.Adicionar("O status de atividade da conta não pode ser inativo");

        if (SaldoTemporario < 0)
            notificacao.Adicionar("Saldo insuficiente.");

        if (valorDebito != decimal.Round(valorDebito, 2))
            notificacao.Adicionar("O saldo possui mais que duas casas decimais");

        if (notificacao.TemNotificacoes)
            return;

        Saldo -= valorDebito;
    }

    public void Desativar(Notificacao notificacao)
    {
        if (StatusAtividadeConta == StatusAtividadeContaEnum.Inativo)
            notificacao.Adicionar("A conta já está desativada.");

        if (Saldo != 0)
            notificacao.Adicionar("Só é possível desativar uma conta com saldo zero.");

        if (notificacao.TemNotificacoes)
            return;

        StatusAtividadeConta = StatusAtividadeContaEnum.Inativo;
    }

    public void Reativar(Notificacao notificacao)
    {
        if (StatusAtividadeConta == StatusAtividadeContaEnum.Ativo)
            notificacao.Adicionar("Não se pode ativar uma conta já ativa");

        if (notificacao.TemNotificacoes)
            return;

        StatusAtividadeConta = StatusAtividadeContaEnum.Ativo;
    }
}
