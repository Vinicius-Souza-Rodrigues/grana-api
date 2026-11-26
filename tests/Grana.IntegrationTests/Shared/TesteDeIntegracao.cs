using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace IntegrationTests.Shared;

[Collection(ApiCollection.Nome)]
public abstract class TesteDeIntegracao(GranaApiFactory factory) : IAsyncLifetime
{
    protected static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.UtcNow);

    protected GranaApiFactory Factory { get; } = factory;
    protected HttpClient Cliente { get; } = factory.CreateClient();

    public Task InitializeAsync() => Factory.LimparBanco();

    public Task DisposeAsync()
    {
        Cliente.Dispose();

        return Task.CompletedTask;
    }

    protected async Task<Guid> CriarConta(string nome, decimal saldoInicial)
    {
        var resposta = await Cliente.PostAsJsonAsync("api/contas", new { nome, tipo = "Corrente", saldoInicial });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        return (await LerDados(resposta)).GetProperty("id").GetGuid();
    }

    protected async Task<Guid> CriarCategoria(string nome, string tipo)
    {
        var resposta = await Cliente.PostAsJsonAsync("api/categorias", new { nome, tipo });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        return (await LerDados(resposta)).GetProperty("id").GetGuid();
    }

    protected async Task<decimal> ObterSaldo(Guid idConta)
    {
        var resposta = await Cliente.GetAsync($"api/contas/{idConta}");

        return (await LerDados(resposta)).GetProperty("saldo").GetDecimal();
    }

    protected static async Task<JsonElement> LerJson(HttpResponseMessage resposta)
        => JsonDocument.Parse(await resposta.Content.ReadAsStringAsync()).RootElement;

    protected static async Task<JsonElement> LerDados(HttpResponseMessage resposta)
        => (await LerJson(resposta)).GetProperty("dados");
}
