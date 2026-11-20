using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Tests.Domain;

public sealed class CategoriaTest
{
    private readonly Notificacao _notificacao = new();

    [Fact]
    public void CriarCategoriaDespesaComOrcamento()
    {
        var categoria = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, 800m, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal(800m, categoria.OrcamentoMensal);
    }

    [Fact]
    public void CriarCategoriaReceitaSemOrcamento()
    {
        var categoria = Categoria.CriacaoCategoria("Salário", TipoCategoriaEnum.Receita, null, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Null(categoria.OrcamentoMensal);
    }

    [Fact]
    public void CriarCategoriaReceitaComOrcamento()
    {
        var categoria = Categoria.CriacaoCategoria("Salário", TipoCategoriaEnum.Receita, 500m, _notificacao);

        Assert.Null(categoria);
        Assert.Contains("Orçamento só pode ser definido para categorias de despesa.", _notificacao.Notificacoes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void CriarCategoriaComOrcamentoInvalido(int orcamento)
    {
        var categoria = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, orcamento, _notificacao);

        Assert.Null(categoria);
        Assert.Contains("O orçamento mensal deve ser maior que zero.", _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarCategoriaComOrcamentoComTresCasasDecimais()
    {
        var categoria = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, 10.555m, _notificacao);

        Assert.Null(categoria);
        Assert.Contains("O orçamento mensal possui mais que duas casas decimais.", _notificacao.Notificacoes);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("Lorem ipsum dolor sit amet consectetur adipiscing e")]
    public void CriarCategoriaComTamanhoDeNomeInvalido(string nome)
    {
        var categoria = Categoria.CriacaoCategoria(nome, TipoCategoriaEnum.Despesa, null, _notificacao);

        Assert.Null(categoria);
        Assert.Contains("O nome não pode ter menos que 2 caracteres ou mais que 50.", _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarCategoriaComNomeNulo()
    {
        var categoria = Categoria.CriacaoCategoria(null, TipoCategoriaEnum.Despesa, null, _notificacao);

        Assert.Null(categoria);
        Assert.Contains("O nome passado é inválido, não pode ser nulo.", _notificacao.Notificacoes);
    }

    [Fact]
    public void CriarCategoriaComEspacos()
    {
        var categoria = Categoria.CriacaoCategoria("  Mercado  ", TipoCategoriaEnum.Despesa, null, _notificacao);

        Assert.NotNull(categoria);
        Assert.Equal("Mercado", categoria.Nome);
    }

    [Fact]
    public void CriarCategoriaComTipoInvalido()
    {
        var categoria = Categoria.CriacaoCategoria("Mercado", (TipoCategoriaEnum)99, null, _notificacao);

        Assert.Null(categoria);
        Assert.Contains("O tipo da categoria passado é inválido.", _notificacao.Notificacoes);
    }

    [Fact]
    public void RemoverOrcamentoDeDespesa()
    {
        var categoria = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, 800m, new Notificacao());

        categoria.DefinirOrcamentoMensal(null, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Null(categoria.OrcamentoMensal);
    }

    [Fact]
    public void DefinirOrcamentoEmReceita()
    {
        var categoria = Categoria.CriacaoCategoria("Salário", TipoCategoriaEnum.Receita, null, new Notificacao());

        categoria.DefinirOrcamentoMensal(500m, _notificacao);

        Assert.Contains("Orçamento só pode ser definido para categorias de despesa.", _notificacao.Notificacoes);
        Assert.Null(categoria.OrcamentoMensal);
    }

    [Fact]
    public void EditarCategoriaComDadosValidos()
    {
        var categoria = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, 800m, new Notificacao());

        categoria.Editar(" Supermercado ", 1000m, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal("Supermercado", categoria.Nome);
        Assert.Equal(1000m, categoria.OrcamentoMensal);
    }

    [Fact]
    public void EditarCategoriaComNomeInvalidoNaoAlteraOrcamento()
    {
        var categoria = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, 800m, new Notificacao());

        categoria.Editar("a", 1000m, _notificacao);

        Assert.True(_notificacao.TemNotificacoes);
        Assert.Equal("Mercado", categoria.Nome);
        Assert.Equal(800m, categoria.OrcamentoMensal);
    }

    [Fact]
    public void EditarCategoriaComOrcamentoInvalidoNaoAlteraNome()
    {
        var categoria = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, 800m, new Notificacao());

        categoria.Editar("Supermercado", 0, _notificacao);

        Assert.True(_notificacao.TemNotificacoes);
        Assert.Equal("Mercado", categoria.Nome);
        Assert.Equal(800m, categoria.OrcamentoMensal);
    }

    [Fact]
    public void ValidarExclusaoSemLancamentos()
    {
        var categoria = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, null, new Notificacao());

        categoria.ValidarExclusao(false, _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
    }

    [Fact]
    public void ValidarExclusaoComLancamentos()
    {
        var categoria = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, null, new Notificacao());

        categoria.ValidarExclusao(true, _notificacao);

        Assert.Contains("Não é possível excluir uma categoria com lançamentos.", _notificacao.Notificacoes);
    }

    [Fact]
    public void RenomearCategoriaComNomeValido()
    {
        var categoria = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, null, new Notificacao());

        categoria.RenomearNome(" Supermercado ", _notificacao);

        Assert.False(_notificacao.TemNotificacoes);
        Assert.Equal("Supermercado", categoria.Nome);
    }

    [Fact]
    public void RenomearCategoriaComNomeInvalido()
    {
        var categoria = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, null, new Notificacao());

        categoria.RenomearNome("a", _notificacao);

        Assert.True(_notificacao.TemNotificacoes);
        Assert.Equal("Mercado", categoria.Nome);
    }
}
