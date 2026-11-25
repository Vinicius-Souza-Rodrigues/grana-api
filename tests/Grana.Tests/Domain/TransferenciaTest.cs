using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Tests.Domain;

public sealed class TransferenciaTest
{
    private readonly Notificacao _notificacao = new();
    private readonly Conta _origem = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 100m, new Notificacao());
    private readonly Conta _destino = Conta.CriacaoConta("Inter", TipoContaEnum.Poupanca, 50m, new Notificacao());

    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.UtcNow);

    private static Conta CriarContaDesativada(string nome)
    {
        var conta = Conta.CriacaoConta(nome, TipoContaEnum.Corrente, 0m, new Notificacao());
        conta.Desativar(new Notificacao());

        return conta;
    }

    [Fact]
    public void CriarTransferenciaValida()
    {
        var transferencia = Transferencia.CriacaoTransferencia(_origem, _destino, 40m, Hoje, "  Reserva  ", _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.NotEqual(Guid.Empty, transferencia.Id);
        Assert.Equal(_origem.Id, transferencia.IdContaOrigem);
        Assert.Equal(_destino.Id, transferencia.IdContaDestino);
        Assert.Equal(40m, transferencia.Valor);
        Assert.Equal(Hoje, transferencia.Data);
        Assert.Equal("Reserva", transferencia.Descricao);
    }

    [Fact]
    public void CriarTransferenciaNaoMovimentaOsSaldos()
    {
        Transferencia.CriacaoTransferencia(_origem, _destino, 40m, Hoje, null, _notificacao);

        Assert.Equal(100m, _origem.Saldo);
        Assert.Equal(50m, _destino.Saldo);
    }

    [Fact]
    public void CriarTransferenciaComSaldoInsuficienteEDestinoDesativado()
    {
        var destino = CriarContaDesativada("Inter");

        var transferencia = Transferencia.CriacaoTransferencia(_origem, destino, 150m, Hoje, null, _notificacao);

        Assert.Null(transferencia);
        Assert.Equal(2, _notificacao.Notificacoes.Count);
        Assert.Contains("Saldo insuficiente.", _notificacao.Notificacoes);
        Assert.Contains("A conta de destino está desativada.", _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarTransferenciaComOrigemIgualAoDestino()
    {
        var transferencia = Transferencia.CriacaoTransferencia(_origem, _origem, 10m, Hoje, null, _notificacao);

        Assert.Null(transferencia);
        Assert.Equal(["A conta de origem e a de destino devem ser diferentes."], _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarTransferenciaComOrigemDesativada()
    {
        var origem = CriarContaDesativada("Nubank");

        var transferencia = Transferencia.CriacaoTransferencia(origem, _destino, 0m, Hoje, null, _notificacao);

        Assert.Null(transferencia);
        Assert.Contains("A conta de origem está desativada.", _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarTransferenciaSemOrigemESemDestino()
    {
        var transferencia = Transferencia.CriacaoTransferencia(null, null, 10m, Hoje, null, _notificacao);

        Assert.Null(transferencia);
        Assert.Equal(2, _notificacao.Notificacoes.Count);
        Assert.Contains("A conta de origem é obrigatória.", _notificacao.Notificacoes);
        Assert.Contains("A conta de destino é obrigatória.", _notificacao.Notificacoes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void CriarTransferenciaComValorInvalido(int valor)
    {
        var transferencia = Transferencia.CriacaoTransferencia(_origem, _destino, valor, Hoje, null, _notificacao);

        Assert.Null(transferencia);
        Assert.Equal(["O valor deve ser maior que zero."], _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarTransferenciaComValorComTresCasasDecimais()
    {
        var transferencia = Transferencia.CriacaoTransferencia(_origem, _destino, 10.555m, Hoje, null, _notificacao);

        Assert.Null(transferencia);
        Assert.Contains("O valor possui mais que duas casas decimais.", _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarTransferenciaComDataFutura()
    {
        var transferencia = Transferencia.CriacaoTransferencia(_origem, _destino, 10m, Hoje.AddDays(1), null, _notificacao);

        Assert.Null(transferencia);
        Assert.Contains("A data da transferência não pode ser futura.", _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarTransferenciaComDescricaoMuitoLonga()
    {
        var transferencia = Transferencia.CriacaoTransferencia(_origem, _destino, 10m, Hoje, new string('a', 101), _notificacao);

        Assert.Null(transferencia);
        Assert.Contains("A descrição não pode ter mais que 100 caracteres.", _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarTransferenciaComTodoOSaldoDaOrigem()
    {
        var transferencia = Transferencia.CriacaoTransferencia(_origem, _destino, 100m, Hoje, null, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.NotNull(transferencia);
    }

    [Fact]
    public void CriarTransferenciaReuneTodosOsErros()
    {
        var origem = CriarContaDesativada("Nubank");
        var destino = CriarContaDesativada("Inter");

        var transferencia = Transferencia.CriacaoTransferencia(origem, destino, 10.555m, Hoje.AddDays(1), new string('a', 101), _notificacao);

        Assert.Null(transferencia);
        Assert.Equal(6, _notificacao.Notificacoes.Count);
    }

    [Fact]
    public void EfetivarTransferenciaMovimentaAsDuasContas()
    {
        var transferencia = Transferencia.CriacaoTransferencia(_origem, _destino, 40m, Hoje, null, new Notificacao());

        transferencia.Efetivar(_notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(60m, _origem.Saldo);
        Assert.Equal(90m, _destino.Saldo);
    }

    [Fact]
    public void EfetivarTransferenciaComDestinoDesativadoDepoisDeCriada()
    {
        var destino = Conta.CriacaoConta("Inter", TipoContaEnum.Poupanca, 0m, new Notificacao());
        var transferencia = Transferencia.CriacaoTransferencia(_origem, destino, 40m, Hoje, null, new Notificacao());
        destino.Desativar(new Notificacao());

        transferencia.Efetivar(_notificacao);

        Assert.True(_notificacao.TemNotificacoes);
        Assert.Equal(100m, _origem.Saldo);
        Assert.Equal(0m, destino.Saldo);
    }

    [Fact]
    public void EfetivarTransferenciaComSaldoGastoDepoisDeCriada()
    {
        var transferencia = Transferencia.CriacaoTransferencia(_origem, _destino, 40m, Hoje, null, new Notificacao());
        _origem.Debitar(80m, new Notificacao());

        transferencia.Efetivar(_notificacao);

        Assert.Equal(["Saldo insuficiente."], _notificacao.Notificacoes);
        Assert.Equal(20m, _origem.Saldo);
        Assert.Equal(50m, _destino.Saldo);
    }

    [Fact]
    public void EstornarTransferenciaDevolveOsSaldos()
    {
        var transferencia = CriarTransferenciaEfetivada(_origem, _destino, 40m);

        transferencia.Estornar(_notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(100m, _origem.Saldo);
        Assert.Equal(50m, _destino.Saldo);
    }

    [Fact]
    public void EstornarTransferenciaComDestinoQueJaGastouOValor()
    {
        var transferencia = CriarTransferenciaEfetivada(_origem, _destino, 40m);
        _destino.Debitar(80m, new Notificacao());

        transferencia.Estornar(_notificacao);

        Assert.Equal(["Saldo insuficiente."], _notificacao.Notificacoes);
        Assert.Equal(60m, _origem.Saldo);
        Assert.Equal(10m, _destino.Saldo);
    }

    [Fact]
    public void EstornarTransferenciaComOrigemDesativada()
    {
        var transferencia = CriarTransferenciaEfetivada(_origem, _destino, 100m);
        _origem.Desativar(new Notificacao());

        transferencia.Estornar(_notificacao);

        Assert.Equal(["A conta de origem está desativada."], _notificacao.Notificacoes);
        Assert.Equal(0m, _origem.Saldo);
        Assert.Equal(150m, _destino.Saldo);
    }

    [Fact]
    public void EstornarTransferenciaComDestinoDesativadoReuneOsErros()
    {
        var transferencia = CriarTransferenciaEfetivada(_origem, _destino, 40m);
        _destino.Debitar(90m, new Notificacao());
        _destino.Desativar(new Notificacao());

        transferencia.Estornar(_notificacao);

        Assert.Equal(2, _notificacao.Notificacoes.Count);
        Assert.Contains("A conta de destino está desativada.", _notificacao.Notificacoes);
        Assert.Contains("Saldo insuficiente.", _notificacao.Notificacoes);
        Assert.Equal(60m, _origem.Saldo);
        Assert.Equal(0m, _destino.Saldo);
    }

    private static Transferencia CriarTransferenciaEfetivada(Conta origem, Conta destino, decimal valor)
    {
        var transferencia = Transferencia.CriacaoTransferencia(origem, destino, valor, Hoje, null, new Notificacao());
        transferencia.Efetivar(new Notificacao());

        return transferencia;
    }
}
