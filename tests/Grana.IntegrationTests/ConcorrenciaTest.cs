using System.Net;
using System.Net.Http.Json;
using Application.Dtos;
using Application.Interfaces;
using Domain.Enums;
using Grana.Domain.Notificacoes;
using Infraestructure.Data;
using IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests;

public sealed class ConcorrenciaTest(GranaApiFactory factory) : TesteDeIntegracao(factory)
{
    private const string MensagemConflito = "A conta foi alterada por outra operação. Tente novamente.";

    [Fact]
    public async Task DoisDebitosSimultaneosUmPassaEOOutroRecebeConflitoSemGravarOLancamento()
    {
        var conta = await CriarConta("Nubank", 100m);
        var categoria = await CriarCategoria("Mercado", "Despesa");
        var notificacaoPrimeiro = new Notificacao();
        var notificacaoSegundo = new Notificacao();

        using var escopoPrimeiro = Factory.Services.CreateScope();
        using var escopoSegundo = Factory.Services.CreateScope();
        await escopoSegundo.ServiceProvider.GetRequiredService<GranaDbContext>().Contas.FirstAsync(c => c.Id == conta);

        await escopoPrimeiro.ServiceProvider.GetRequiredService<ILancamentoService>()
            .CriarLancamento(new LancamentoCriarDto(conta, categoria, 80m, Hoje, null), notificacaoPrimeiro, CancellationToken.None);
        await escopoSegundo.ServiceProvider.GetRequiredService<ILancamentoService>()
            .CriarLancamento(new LancamentoCriarDto(conta, categoria, 80m, Hoje, null), notificacaoSegundo, CancellationToken.None);

        var lancamentos = await LerDados(await Cliente.GetAsync($"api/lancamentos?contaId={conta}"));

        Assert.False(notificacaoPrimeiro.TemNotificacoes);
        Assert.Equal(TipoNotificacaoEnum.Conflito, notificacaoSegundo.Tipo);
        Assert.Equal(MensagemConflito, Assert.Single(notificacaoSegundo.Notificacoes));
        Assert.Equal(20m, await ObterSaldo(conta));
        Assert.Equal(1, lancamentos.GetProperty("totalItens").GetInt32());
    }

    [Fact]
    public async Task DebitosEmParaleloNuncaDeixamOSaldoNegativo()
    {
        var conta = await CriarConta("Nubank", 100m);
        var categoria = await CriarCategoria("Mercado", "Despesa");

        var requisicoes = Enumerable.Range(0, 10)
            .Select(_ => Cliente.PostAsJsonAsync("api/lancamentos", new { contaId = conta, categoriaId = categoria, valor = 30m, data = Hoje, descricao = (string)null }));
        var respostas = await Task.WhenAll(requisicoes);

        var aprovados = respostas.Count(r => r.StatusCode == HttpStatusCode.Created);
        var lancamentos = await LerDados(await Cliente.GetAsync($"api/lancamentos?contaId={conta}"));

        Assert.All(respostas, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.Conflict, HttpStatusCode.BadRequest }));
        Assert.InRange(aprovados, 1, 3);
        Assert.Equal(100m - aprovados * 30m, await ObterSaldo(conta));
        Assert.Equal(aprovados, lancamentos.GetProperty("totalItens").GetInt32());
    }
}
