using System.Net;
using System.Net.Http.Json;
using Infraestructure.Data;
using IntegrationTests.Shared;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests;

public sealed class TransferenciaAtomicidadeTest(GranaApiFactory factory) : TesteDeIntegracao(factory)
{
    [Fact]
    public async Task FalhaAoGravarATransferenciaNaoAlteraOSaldoDeNenhumaConta()
    {
        var origem = await CriarConta("Nubank", 1000m);
        var destino = await CriarConta("Inter", 0m);

        using var factoryComFalha = Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            s.ConfigureDbContext<GranaDbContext>(o => o.AddInterceptors(new FalhaAoGravarTransferencia()))));
        using var clienteComFalha = factoryComFalha.CreateClient();

        var resposta = await clienteComFalha.PostAsJsonAsync("api/transferencias", new { contaOrigemId = origem, contaDestinoId = destino, valor = 300m, data = Hoje, descricao = (string)null });

        var transferencias = await LerDados(await Cliente.GetAsync("api/transferencias"));

        Assert.Equal(HttpStatusCode.InternalServerError, resposta.StatusCode);
        Assert.Equal(1000m, await ObterSaldo(origem));
        Assert.Equal(0m, await ObterSaldo(destino));
        Assert.Equal(0, transferencias.GetArrayLength());
    }

    [Fact]
    public async Task TransferenciaComSaldoInsuficienteNaoAlteraOSaldoDeNenhumaConta()
    {
        var origem = await CriarConta("Nubank", 100m);
        var destino = await CriarConta("Inter", 0m);

        var resposta = await Cliente.PostAsJsonAsync("api/transferencias", new { contaOrigemId = origem, contaDestinoId = destino, valor = 300m, data = Hoje, descricao = (string)null });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(100m, await ObterSaldo(origem));
        Assert.Equal(0m, await ObterSaldo(destino));
    }
}
