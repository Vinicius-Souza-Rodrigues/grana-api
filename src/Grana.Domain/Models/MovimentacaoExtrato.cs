using Domain.Enums;

namespace Domain.Models;

public sealed class MovimentacaoExtrato
{
    public MovimentacaoExtrato(Guid id, TipoMovimentacaoEnum tipo, decimal valor, DateOnly data, string descricao, DateTime criadoEm)
    {
        Id = id;
        Tipo = tipo;
        Valor = valor;
        Data = data;
        Descricao = descricao;
        CriadoEm = criadoEm;
    }

    public Guid Id { get; private set; }
    public TipoMovimentacaoEnum Tipo { get; private set; }
    public decimal Valor { get; private set; }
    public DateOnly Data { get; private set; }
    public string Descricao { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public decimal SaldoApos { get; private set; }
    public bool EhEntrada => Tipo == TipoMovimentacaoEnum.Receita || Tipo == TipoMovimentacaoEnum.TransferenciaRecebida;
    public decimal ValorComSinal => EhEntrada ? Valor : -Valor;

    public void DefinirSaldoApos(decimal saldoApos)
    {
        SaldoApos = saldoApos;
    }
}
