using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Tests.Domain;

public sealed class ContaTest
{
    private readonly Notificacao _notificacao = new(); 

    [Fact]
    public void CriarNomeInvalido()
    {
        var conta = Conta.CriacaoConta("ab", (TipoContaEnum)99, -10, _notificacao);

        Assert.Null(conta);
        Assert.Equal(3, _notificacao.Notificacoes.Count);
    }

    [Fact]
    public void DebitarSaldoInvalido()
    {
        var conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 100m, new Notificacao());

        conta.Debitar(100.01m, _notificacao);

        Assert.Contains("Saldo insuficiente.", _notificacao.Notificacoes);
        Assert.Equal(100m, conta.Saldo);
    }

    [Fact]
    public void DebitarSaldoValido()
    {
         var conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 100m, new Notificacao());

         conta.Debitar(100, _notificacao);

         Assert.Equal(0m, conta.Saldo);
    }

    [Fact]
    public void DesativarContaInvalida()
    {
        var conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 0.01m, new Notificacao());

        conta.Desativar(_notificacao);

        Assert.Contains("Só é possível desativar uma conta com saldo zero.", _notificacao.Notificacoes);
        Assert.Equal(StatusAtividadeContaEnum.Ativo, conta.StatusAtividadeConta);
    }

    [Fact]
    public void CreditarContaInvalida()
    {
        var conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 0, new Notificacao());

        conta.Desativar(new Notificacao());

        conta.Creditar(100, _notificacao);

        Assert.Equal(0m, conta.Saldo);
        Assert.Contains("O status de atividade da conta não pode ser inativo", _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarContaComEspacos()
    {
        var conta = Conta.CriacaoConta(" Nubank ", TipoContaEnum.Corrente, 0, _notificacao);

        Assert.NotNull(conta);
        Assert.Equal("Nubank", conta.Nome);
    }

    [Fact]
    public void CriarContaComDecimais()
    {
        var conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 10.123m, new Notificacao());

        Assert.Null(conta);
    }

    [Fact]
    public void CreditoComDecimais()
    {
        var conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 100m, new Notificacao());

        conta.Creditar(99.001m, _notificacao);

        Assert.True(_notificacao.TemNotificacoes);
        Assert.Equal(100m, conta.Saldo);
    }

    [Fact]
    public void DebitoComDecimais()
    {
        var conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 100m, new Notificacao());

        conta.Debitar(99.001m, _notificacao);

        Assert.True(_notificacao.TemNotificacoes);
        Assert.Equal(100m, conta.Saldo);
    }

    [Fact]
    public void CreditarComValorZero()
    {
        var conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 100m, new Notificacao());

        conta.Creditar(0m, _notificacao);

        Assert.True(_notificacao.TemNotificacoes);
        Assert.Equal(100m, conta.Saldo);
    }
    
    [Fact]
    public void DebitarComValorZero()
    {
        var conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 100m, new Notificacao());

        conta.Debitar(0m, _notificacao);

        Assert.True(_notificacao.TemNotificacoes);
        Assert.Equal(100m, conta.Saldo);
    }

    [Fact]
    public void DesativarContaDesativada()
    {
        var conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 0m, new Notificacao());

        conta.Desativar(new Notificacao());

        conta.Desativar(_notificacao);

        Assert.Contains("A conta já está desativada.", _notificacao.Notificacoes);
    }

    [Fact]
    public void ReativarContaAtiva()
    {
        var conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 0m, new Notificacao());

        conta.Reativar(_notificacao);

        Assert.True(_notificacao.TemNotificacoes);
        Assert.Equal(StatusAtividadeContaEnum.Ativo, conta.StatusAtividadeConta);
    }

    [Fact]
    public void RenomearContaInativa()
    {
        var conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 0m, new Notificacao());
        conta.Desativar(new Notificacao());

        conta.RenomearNome("Novo nome", _notificacao);

        Assert.True(_notificacao.TemNotificacoes);
        Assert.Equal("Nubank", conta.Nome);
    }
}
