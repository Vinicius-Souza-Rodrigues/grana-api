using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Data;

public sealed class SeedDemo(GranaDbContext contexto)
{
    public async Task Executar(DateOnly hoje, CancellationToken cancellationToken)
    {
        if (await contexto.Contas.AnyAsync(cancellationToken))
            return;

        var notificacao = new Notificacao();
        var mesAtual = new DateOnly(hoje.Year, hoje.Month, 1);
        var mesAnterior = mesAtual.AddMonths(-1);

        var nubank = Conta.CriacaoConta("Nubank", TipoContaEnum.Corrente, 1200m, notificacao);
        var inter = Conta.CriacaoConta("Inter", TipoContaEnum.Poupanca, 5000m, notificacao);
        var carteira = Conta.CriacaoConta("Carteira", TipoContaEnum.Carteira, 150m, notificacao);

        var salario = Categoria.CriacaoCategoria("Salário", TipoCategoriaEnum.Receita, null, notificacao);
        var freelas = Categoria.CriacaoCategoria("Freelas", TipoCategoriaEnum.Receita, null, notificacao);
        var mercado = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, 1200m, notificacao);
        var aluguel = Categoria.CriacaoCategoria("Aluguel", TipoCategoriaEnum.Despesa, 1800m, notificacao);
        var transporte = Categoria.CriacaoCategoria("Transporte", TipoCategoriaEnum.Despesa, 300m, notificacao);
        var lazer = Categoria.CriacaoCategoria("Lazer", TipoCategoriaEnum.Despesa, 400m, notificacao);

        if (notificacao.TemNotificacoes)
            throw new InvalidOperationException(string.Join(" ", notificacao.Notificacoes));

        Lancar(nubank, salario, 5000m, Dia(mesAnterior, 5, hoje), "Salário", notificacao);
        Lancar(nubank, aluguel, 1800m, Dia(mesAnterior, 6, hoje), "Aluguel", notificacao);
        Lancar(nubank, mercado, 650m, Dia(mesAnterior, 8, hoje), "Compra do mês", notificacao);
        Lancar(nubank, transporte, 120m, Dia(mesAnterior, 10, hoje), "Combustível", notificacao);
        Lancar(carteira, lazer, 40m, Dia(mesAnterior, 12, hoje), "Sorvete", notificacao);
        Lancar(nubank, lazer, 180m, Dia(mesAnterior, 15, hoje), "Cinema e jantar", notificacao);
        Transferir(nubank, inter, 1000m, Dia(mesAnterior, 20, hoje), "Reserva de emergência", notificacao);
        Lancar(nubank, freelas, 1500m, Dia(mesAnterior, 22, hoje), "Site para cliente", notificacao);
        Lancar(nubank, mercado, 480m, Dia(mesAnterior, 25, hoje), "Mercado da semana", notificacao);

        Lancar(nubank, salario, 5000m, Dia(mesAtual, 5, hoje), "Salário", notificacao);
        Lancar(nubank, aluguel, 1800m, Dia(mesAtual, 6, hoje), "Aluguel", notificacao);
        Lancar(nubank, mercado, 650m, Dia(mesAtual, 8, hoje), "Compra do mês", notificacao);
        Lancar(nubank, transporte, 95m, Dia(mesAtual, 10, hoje), "Combustível", notificacao);
        Lancar(carteira, lazer, 35m, Dia(mesAtual, 12, hoje), "Açaí", notificacao);
        Lancar(nubank, mercado, 350m, Dia(mesAtual, 14, hoje), "Mercado da semana", notificacao);
        Transferir(nubank, inter, 1000m, Dia(mesAtual, 20, hoje), "Reserva de emergência", notificacao);

        if (notificacao.TemNotificacoes)
            throw new InvalidOperationException(string.Join(" ", notificacao.Notificacoes));

        contexto.Contas.AddRange(nubank, inter, carteira);
        contexto.Categorias.AddRange(salario, freelas, mercado, aluguel, transporte, lazer);

        await contexto.SaveChangesAsync(cancellationToken);
    }

    private void Lancar(Conta conta, Categoria categoria, decimal valor, DateOnly data, string descricao, Notificacao notificacao)
    {
        var lancamento = Lancamento.CriacaoLancamento(conta, categoria, valor, data, descricao, notificacao);

        if (lancamento is null)
            return;

        conta.AplicarLancamento(lancamento, notificacao);
        contexto.Lancamentos.Add(lancamento);
    }

    private void Transferir(Conta origem, Conta destino, decimal valor, DateOnly data, string descricao, Notificacao notificacao)
    {
        var transferencia = Transferencia.CriacaoTransferencia(origem, destino, valor, data, descricao, notificacao);

        if (transferencia is null)
            return;

        transferencia.Efetivar(notificacao);
        contexto.Transferencias.Add(transferencia);
    }

    private static DateOnly Dia(DateOnly inicioDoMes, int dia, DateOnly hoje)
    {
        var data = inicioDoMes.AddDays(dia - 1);

        if (data > hoje)
            return hoje;

        return data;
    }
}
