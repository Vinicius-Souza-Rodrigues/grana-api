using Domain.Enums;
using Grana.Domain.Notificacoes;

namespace Domain.Models;

public sealed class Categoria
{
    private Categoria()
    {
    }

    private Categoria(string nome, TipoCategoriaEnum tipoCategoria, decimal? orcamentoMensal)
    {
        Id = Guid.NewGuid();
        Nome = nome;
        TipoCategoria = tipoCategoria;
        OrcamentoMensal = orcamentoMensal;
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public TipoCategoriaEnum TipoCategoria { get; private set; }
    public decimal? OrcamentoMensal { get; private set; }

    public const int TamanhoMinimoNome = 2;
    public const int TamanhoMaximoNome = 50;
    public const decimal PercentualAlertaOrcamento = 0.8m;

    public static Categoria CriacaoCategoria(string nome, TipoCategoriaEnum tipoCategoria, decimal? orcamentoMensal, Notificacao notificacao)
    {
        nome = nome?.Trim();

        ValidarNome(nome, notificacao);

        if (!Enum.IsDefined(tipoCategoria))
            notificacao.Adicionar("O tipo da categoria passado é inválido.");

        ValidarOrcamento(tipoCategoria, orcamentoMensal, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        return new Categoria(nome, tipoCategoria, orcamentoMensal);
    }

    public void Editar(string nome, decimal? orcamentoMensal, Notificacao notificacao)
    {
        nome = nome?.Trim();

        ValidarNome(nome, notificacao);

        ValidarOrcamento(TipoCategoria, orcamentoMensal, notificacao);

        if (notificacao.TemNotificacoes)
            return;

        Nome = nome;
        OrcamentoMensal = orcamentoMensal;
    }

    public void RenomearNome(string nome, Notificacao notificacao)
    {
        nome = nome?.Trim();

        ValidarNome(nome, notificacao);

        if (notificacao.TemNotificacoes)
            return;

        Nome = nome;
    }

    public void DefinirOrcamentoMensal(decimal? orcamentoMensal, Notificacao notificacao)
    {
        ValidarOrcamento(TipoCategoria, orcamentoMensal, notificacao);

        if (notificacao.TemNotificacoes)
            return;

        OrcamentoMensal = orcamentoMensal;
    }

    public void ValidarExclusao(bool possuiLancamentos, Notificacao notificacao)
    {
        if (possuiLancamentos)
            notificacao.Adicionar("Não é possível excluir uma categoria com lançamentos.");
    }

    public void VerificarOrcamento(decimal totalDespesasNoMes, Notificacao notificacao)
    {
        if (OrcamentoMensal is null)
            return;

        if (totalDespesasNoMes > OrcamentoMensal.Value)
            notificacao.AdicionarAviso($"O orçamento da categoria {Nome} foi ultrapassado.");

        else if (totalDespesasNoMes > OrcamentoMensal.Value * PercentualAlertaOrcamento)
            notificacao.AdicionarAviso($"A categoria {Nome} passou de 80% do orçamento.");
    }

    private static void ValidarNome(string nome, Notificacao notificacao)
    {
        if (string.IsNullOrWhiteSpace(nome))
            notificacao.Adicionar("O nome passado é inválido, não pode ser nulo.");

        else if (nome.Length < TamanhoMinimoNome || nome.Length > TamanhoMaximoNome)
            notificacao.Adicionar("O nome não pode ter menos que 2 caracteres ou mais que 50.");
    }

    private static void ValidarOrcamento(TipoCategoriaEnum tipoCategoria, decimal? orcamentoMensal, Notificacao notificacao)
    {
        if (orcamentoMensal is null)
            return;

        if (tipoCategoria != TipoCategoriaEnum.Despesa)
            notificacao.Adicionar("Orçamento só pode ser definido para categorias de despesa.");

        if (orcamentoMensal <= 0)
            notificacao.Adicionar("O orçamento mensal deve ser maior que zero.");

        if (orcamentoMensal.Value != decimal.Round(orcamentoMensal.Value, 2))
            notificacao.Adicionar("O orçamento mensal possui mais que duas casas decimais.");
    }
}
