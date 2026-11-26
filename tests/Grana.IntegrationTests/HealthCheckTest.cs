using System.Net;
using IntegrationTests.Shared;
using Microsoft.AspNetCore.Hosting;

namespace IntegrationTests;

public sealed class HealthCheckTest(GranaApiFactory factory) : TesteDeIntegracao(factory)
{
    [Fact]
    public async Task BancoNoArRespondeHealthy()
    {
        var resposta = await Cliente.GetAsync("health");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Healthy", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task BancoForaRespondeUnhealthy()
    {
        using var factoryBancoFora = Factory.WithWebHostBuilder(b =>
            b.UseSetting("ConnectionStrings:Grana", "Host=localhost;Port=1;Database=grana;Username=grana;Password=grana;Timeout=3"));
        using var clienteBancoFora = factoryBancoFora.CreateClient();

        var resposta = await clienteBancoFora.GetAsync("health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        Assert.Equal("Unhealthy", await resposta.Content.ReadAsStringAsync());
    }
}
