using Domain.Enums;
using Grana.Domain.Notificacoes;

namespace Domain.Models;

public sealed class Lancamento
{
    private Lancamento()
    {
    }

    private Lancamento(Conta conta, Categoria categoria, decimal valor, DateOnly data, string descricao)
    {
        Id = Guid.NewGuid();
        IdConta = conta.Id;
        Conta = conta;
        IdCategoria = categoria.Id;
        Categoria = categoria;
        TipoLancamento = categoria.TipoCategoria;
        Valor = valor;
        Data = data;
        Descricao = descricao;
        CriadoEm = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid IdConta { get; private set; }
    public Conta Conta { get; private set; }
    public Guid IdCategoria { get; private set; }
    public Categoria Categoria { get; private set; }
    public TipoCategoriaEnum TipoLancamento { get; private set; }
    public decimal Valor { get; private set; }
    public DateOnly Data { get; private set; }
    public string Descricao { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public const int TamanhoMaximoDescricao = 100;

    public static Lancamento CriacaoLancamento(Conta conta, Categoria categoria, decimal valor, DateOnly data, string descricao, Notificacao notificacao)
    {
        descricao = NormalizarDescricao(descricao);

        if (conta is null)
            notificacao.Adicionar("A conta do lançamento é obrigatória.");

        if (categoria is null)
            notificacao.Adicionar("A categoria do lançamento é obrigatória.");

        ValidarDados(valor, data, descricao, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        return new Lancamento(conta, categoria, valor, data, descricao);
    }

    public void Editar(Categoria categoria, decimal valor, DateOnly data, string descricao, Notificacao notificacao)
    {
        descricao = NormalizarDescricao(descricao);

        ValidarNovaCategoria(categoria, notificacao);

        ValidarDados(valor, data, descricao, notificacao);

        if (notificacao.TemNotificacoes)
            return;

        Conta.SubstituirValorDeLancamento(TipoLancamento, Valor, valor, notificacao);

        if (notificacao.TemNotificacoes)
            return;

        IdCategoria = categoria.Id;
        Categoria = categoria;
        Valor = valor;
        Data = data;
        Descricao = descricao;
    }

    public void AlterarCategoria(Categoria categoria, Notificacao notificacao)
    {
        ValidarNovaCategoria(categoria, notificacao);

        if (notificacao.TemNotificacoes)
            return;

        IdCategoria = categoria.Id;
        Categoria = categoria;
    }

    private void ValidarNovaCategoria(Categoria categoria, Notificacao notificacao)
    {
        if (categoria is null)
            notificacao.Adicionar("A categoria do lançamento é obrigatória.");

        else if (categoria.TipoCategoria != TipoLancamento)
            notificacao.Adicionar("A nova categoria precisa ser do mesmo tipo do lançamento.");
    }

    private static string NormalizarDescricao(string descricao)
    {
        descricao = descricao?.Trim();

        if (string.IsNullOrEmpty(descricao))
            return null;

        return descricao;
    }

    private static void ValidarDados(decimal valor, DateOnly data, string descricao, Notificacao notificacao)
    {
        if (valor <= 0)
            notificacao.Adicionar("O valor deve ser maior que zero.");

        if (valor != decimal.Round(valor, 2))
            notificacao.Adicionar("O valor possui mais que duas casas decimais.");

        if (data > DateOnly.FromDateTime(DateTime.UtcNow))
            notificacao.Adicionar("A data do lançamento não pode ser futura.");

        if (descricao is not null && descricao.Length > TamanhoMaximoDescricao)
            notificacao.Adicionar("A descrição não pode ter mais que 100 caracteres.");
    }
}
