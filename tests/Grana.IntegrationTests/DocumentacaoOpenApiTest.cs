using System.Net;
using System.Text.Json;
using IntegrationTests.Shared;

namespace IntegrationTests;

public sealed class DocumentacaoOpenApiTest(GranaApiFactory factory) : TesteDeIntegracao(factory)
{
    private const string TipoJson = "application/json";

    private static readonly string[] MetodosHttp = ["get", "post", "put", "patch", "delete"];

    [Fact]
    public async Task TodosOsEndpointsTemResumoEExemploEmCadaRespostaComCorpo()
    {
        var operacoes = ListarOperacoes(await ObterDocumento()).ToList();

        Assert.Equal(23, operacoes.Count);

        foreach (var (rota, operacao) in operacoes)
        {
            Assert.False(string.IsNullOrWhiteSpace(operacao.GetProperty("summary").GetString()), $"{rota} sem resumo");

            foreach (var resposta in operacao.GetProperty("responses").EnumerateObject())
            {
                if (resposta.Name == "204")
                    continue;

                Assert.True(TemExemplo(resposta.Value), $"{rota} sem exemplo na resposta {resposta.Name}");
            }

            if (operacao.TryGetProperty("requestBody", out var corpo))
                Assert.True(TemExemplo(corpo), $"{rota} sem exemplo no corpo da requisição");
        }
    }

    [Fact]
    public async Task EndpointsDocumentamOsCodigosDeRespostaPossiveis()
    {
        var caminhos = (await ObterDocumento()).GetProperty("paths");

        Assert.Equal(["201", "400", "404", "409"], Codigos(caminhos, "/api/lancamentos", "post"));
        Assert.Equal(["200", "404"], Codigos(caminhos, "/api/contas/{id}", "get"));
        Assert.Equal(["204", "400", "404"], Codigos(caminhos, "/api/categorias/{id}", "delete"));
        Assert.Equal(["200", "400", "404"], Codigos(caminhos, "/api/contas/{id}/extrato", "get"));
    }

    [Fact]
    public async Task DocumentoTemOEsquemaBearerConfigurado()
    {
        var documento = await ObterDocumento();
        var bearer = documento.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");

        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Equal("JWT", bearer.GetProperty("bearerFormat").GetString());
        Assert.True(documento.GetProperty("security")[0].TryGetProperty("Bearer", out _));
    }

    [Fact]
    public async Task ExemploDeErroSegueOFormatoDaApi()
    {
        var caminhos = (await ObterDocumento()).GetProperty("paths");
        var exemplo = caminhos.GetProperty("/api/contas").GetProperty("post").GetProperty("responses").GetProperty("409")
            .GetProperty("content").GetProperty(TipoJson).GetProperty("example");

        Assert.Equal(409, exemplo.GetProperty("codigo").GetInt32());
        Assert.Equal("Não foi possível concluir a operação.", exemplo.GetProperty("mensagem").GetString());
        Assert.Equal("Já existe uma conta ativa com esse nome.", exemplo.GetProperty("erros")[0].GetString());
    }

    [Fact]
    public async Task ScalarEstaDisponivel()
    {
        var resposta = await Cliente.GetAsync("scalar");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    private async Task<JsonElement> ObterDocumento()
    {
        var resposta = await Cliente.GetAsync("openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        return await LerJson(resposta);
    }

    private static IEnumerable<(string Rota, JsonElement Operacao)> ListarOperacoes(JsonElement documento)
    {
        foreach (var caminho in documento.GetProperty("paths").EnumerateObject())
        {
            foreach (var metodo in caminho.Value.EnumerateObject())
            {
                if (MetodosHttp.Contains(metodo.Name))
                    yield return ($"{metodo.Name.ToUpper()} {caminho.Name}", metodo.Value);
            }
        }
    }

    private static bool TemExemplo(JsonElement elemento)
        => elemento.TryGetProperty("content", out var conteudo)
            && conteudo.TryGetProperty(TipoJson, out var json)
            && json.TryGetProperty("example", out _);

    private static string[] Codigos(JsonElement caminhos, string rota, string metodo)
        => caminhos.GetProperty(rota).GetProperty(metodo).GetProperty("responses").EnumerateObject().Select(r => r.Name).Order().ToArray();
}
