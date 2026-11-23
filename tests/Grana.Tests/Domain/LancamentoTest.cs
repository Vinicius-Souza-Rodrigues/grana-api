using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Tests.Domain;

public sealed class LancamentoTest
{
    private readonly Notificacao _notificacao = new();
    private readonly Conta _conta = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 100m, new Notificacao());
    private readonly Categoria _despesa = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, null, new Notificacao());
    private readonly Categoria _receita = Categoria.CriacaoCategoria("Salário", TipoCategoriaEnum.Receita, null, new Notificacao());

    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void CriarLancamentoValido()
    {
        var lancamento = Lancamento.CriacaoLancamento(_conta, _despesa, 150.50m, Hoje, "Compra do mês", _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(_conta.Id, lancamento.IdConta);
        Assert.Equal(_despesa.Id, lancamento.IdCategoria);
        Assert.Equal(150.50m, lancamento.Valor);
        Assert.Equal(Hoje, lancamento.Data);
        Assert.Equal("Compra do mês", lancamento.Descricao);
    }

    [Fact]
    public void CriarLancamentoComCategoriaDeDespesaTemTipoDespesa()
    {
        var lancamento = Lancamento.CriacaoLancamento(_conta, _despesa, 10, Hoje, null, _notificacao);

        Assert.Equal(TipoCategoriaEnum.Despesa, lancamento.TipoLancamento);
    }

    [Fact]
    public void CriarLancamentoComCategoriaDeReceitaTemTipoReceita()
    {
        var lancamento = Lancamento.CriacaoLancamento(_conta, _receita, 10, Hoje, null, _notificacao);

        Assert.Equal(TipoCategoriaEnum.Receita, lancamento.TipoLancamento);
    }

    [Fact]
    public void CriarLancamentoComTresErros()
    {
        var descricao = new string('a', 101);

        var lancamento = Lancamento.CriacaoLancamento(_conta, _despesa, 0, Hoje.AddDays(1), descricao, _notificacao);

        Assert.Null(lancamento);
        Assert.Equal(3, _notificacao.Notificacoes.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void CriarLancamentoComValorInvalido(int valor)
    {
        var lancamento = Lancamento.CriacaoLancamento(_conta, _despesa, valor, Hoje, null, _notificacao);

        Assert.Null(lancamento);
        Assert.Contains("O valor deve ser maior que zero.", _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarLancamentoComValorComTresCasasDecimais()
    {
        var lancamento = Lancamento.CriacaoLancamento(_conta, _despesa, 10.555m, Hoje, null, _notificacao);

        Assert.Null(lancamento);
        Assert.Contains("O valor possui mais que duas casas decimais.", _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarLancamentoComDataFutura()
    {
        var lancamento = Lancamento.CriacaoLancamento(_conta, _despesa, 10, Hoje.AddDays(1), null, _notificacao);

        Assert.Null(lancamento);
        Assert.Contains("A data do lançamento não pode ser futura.", _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarLancamentoComDataPassada()
    {
        var lancamento = Lancamento.CriacaoLancamento(_conta, _despesa, 10, Hoje.AddDays(-30), null, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.NotNull(lancamento);
    }

    [Fact]
    public void CriarLancamentoComDescricaoDe100Caracteres()
    {
        var lancamento = Lancamento.CriacaoLancamento(_conta, _despesa, 10, Hoje, new string('a', 100), _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(100, lancamento.Descricao.Length);
    }

    [Fact]
    public void CriarLancamentoComDescricaoMuitoLonga()
    {
        var lancamento = Lancamento.CriacaoLancamento(_conta, _despesa, 10, Hoje, new string('a', 101), _notificacao);

        Assert.Null(lancamento);
        Assert.Contains("A descrição não pode ter mais que 100 caracteres.", _notificacao.Notificacoes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void CriarLancamentoSemDescricao(string descricao)
    {
        var lancamento = Lancamento.CriacaoLancamento(_conta, _despesa, 10, Hoje, descricao, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Null(lancamento.Descricao);
    }

    [Fact]
    public void CriarLancamentoSemCategoria()
    {
        var lancamento = Lancamento.CriacaoLancamento(_conta, null, 10, Hoje, null, _notificacao);

        Assert.Null(lancamento);
        Assert.Contains("A categoria do lançamento é obrigatória.", _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarLancamentoSemConta()
    {
        var lancamento = Lancamento.CriacaoLancamento(null, _despesa, 10, Hoje, null, _notificacao);

        Assert.Null(lancamento);
        Assert.Contains("A conta do lançamento é obrigatória.", _notificacao.Notificacoes);
    }

    [Fact]
    public void AlterarCategoriaParaOutraDoMesmoTipo()
    {
        var lancamento = Lancamento.CriacaoLancamento(_conta, _despesa, 10, Hoje, null, new Notificacao());
        var transporte = Categoria.CriacaoCategoria("Transporte", TipoCategoriaEnum.Despesa, null, new Notificacao());

        lancamento.AlterarCategoria(transporte, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(transporte.Id, lancamento.IdCategoria);
    }

    [Fact]
    public void AlterarCategoriaParaOutroTipo()
    {
        var lancamento = Lancamento.CriacaoLancamento(_conta, _despesa, 10, Hoje, null, new Notificacao());

        lancamento.AlterarCategoria(_receita, _notificacao);

        Assert.Contains("A nova categoria precisa ser do mesmo tipo do lançamento.", _notificacao.Notificacoes);
        Assert.Equal(_despesa.Id, lancamento.IdCategoria);
        Assert.Equal(TipoCategoriaEnum.Despesa, lancamento.TipoLancamento);
    }
}
