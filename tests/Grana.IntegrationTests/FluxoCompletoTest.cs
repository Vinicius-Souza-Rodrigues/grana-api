using System.Net;
using System.Net.Http.Json;
using IntegrationTests.Shared;

namespace IntegrationTests;

public sealed class FluxoCompletoTest(GranaApiFactory factory) : TesteDeIntegracao(factory)
{
    [Fact]
    public async Task CriarContasLancarTransferirEConsultarOExtrato()
    {
        var nubank = await CriarConta("Nubank", 1000m);
        var inter = await CriarConta("Inter", 0m);
        var salario = await CriarCategoria("Salário", "Receita");
        var mercado = await CriarCategoria("Mercado", "Despesa");

        var receita = await Cliente.PostAsJsonAsync("api/lancamentos", new { contaId = nubank, categoriaId = salario, valor = 2000m, data = Hoje, descricao = "Salário do mês" });
        var despesa = await Cliente.PostAsJsonAsync("api/lancamentos", new { contaId = nubank, categoriaId = mercado, valor = 500m, data = Hoje, descricao = "Feira" });
        var transferencia = await Cliente.PostAsJsonAsync("api/transferencias", new { contaOrigemId = nubank, contaDestinoId = inter, valor = 700m, data = Hoje, descricao = "Reserva" });

        Assert.Equal(HttpStatusCode.Created, receita.StatusCode);
        Assert.Equal(3000m, (await LerDados(receita)).GetProperty("saldoConta").GetDecimal());
        Assert.Equal(HttpStatusCode.Created, despesa.StatusCode);
        Assert.Equal(2500m, (await LerDados(despesa)).GetProperty("saldoConta").GetDecimal());
        Assert.Equal(HttpStatusCode.Created, transferencia.StatusCode);
        Assert.Equal(1800m, (await LerDados(transferencia)).GetProperty("saldoContaOrigem").GetDecimal());
        Assert.Equal(700m, (await LerDados(transferencia)).GetProperty("saldoContaDestino").GetDecimal());

        var resposta = await Cliente.GetAsync($"api/contas/{nubank}/extrato");
        var extrato = await LerDados(resposta);
        var movimentacoes = extrato.GetProperty("movimentacoes");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(1000m, extrato.GetProperty("saldoAnterior").GetDecimal());
        Assert.Equal(2000m, extrato.GetProperty("totalEntradas").GetDecimal());
        Assert.Equal(1200m, extrato.GetProperty("totalSaidas").GetDecimal());
        Assert.Equal(1800m, extrato.GetProperty("saldoFinal").GetDecimal());
        Assert.Equal(3, movimentacoes.GetArrayLength());
        Assert.Equal("Receita", movimentacoes[0].GetProperty("tipo").GetString());
        Assert.Equal("Despesa", movimentacoes[1].GetProperty("tipo").GetString());
        Assert.Equal("TransferenciaEnviada", movimentacoes[2].GetProperty("tipo").GetString());
        Assert.Equal(-700m, movimentacoes[2].GetProperty("valor").GetDecimal());
        Assert.Equal(1800m, movimentacoes[2].GetProperty("saldoApos").GetDecimal());
        Assert.Equal(1800m, await ObterSaldo(nubank));

        var extratoInter = await LerDados(await Cliente.GetAsync($"api/contas/{inter}/extrato"));

        Assert.Equal(700m, extratoInter.GetProperty("totalEntradas").GetDecimal());
        Assert.Equal(700m, extratoInter.GetProperty("saldoFinal").GetDecimal());
        Assert.Equal("TransferenciaRecebida", extratoInter.GetProperty("movimentacoes")[0].GetProperty("tipo").GetString());
        Assert.Equal(700m, await ObterSaldo(inter));
    }

    [Fact]
    public async Task CadaTesteComecaComOBancoLimpo()
    {
        var resposta = await Cliente.GetAsync("api/contas?incluirInativas=true");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal(0, (await LerDados(resposta)).GetArrayLength());
    }

    [Fact]
    public async Task ContaComNomeRepetidoRecebeConflito()
    {
        await CriarConta("Nubank", 0m);

        var resposta = await Cliente.PostAsJsonAsync("api/contas", new { nome = "nubank", tipo = "Corrente", saldoInicial = 0m });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }
}
