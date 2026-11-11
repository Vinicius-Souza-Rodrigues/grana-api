using System.Text.Json;
using System.Text.Json.Serialization;
using Grana.Api.Controllers;
using Grana.Domain.Notificacoes;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Api;

public sealed class ApiControllerTest
{
    private sealed class ControllerDeTeste : ApiController
    {
        public IActionResult ChamarResponder(Notificacao notificacao, int codigo, string mensagem, object dados = null)
            => Responder(notificacao, codigo, mensagem, dados);
    }

    private static readonly JsonSerializerOptions _opcoesJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly ControllerDeTeste _controller = new();
    private readonly Notificacao _notificacao = new();

    private static JsonElement LerJson(ObjectResult resultado)
        => JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value, _opcoesJson)).RootElement;

    [Fact]
    public void ResponderComDoisErros()
    {
        _notificacao.Adicionar("Erro 1");
        _notificacao.Adicionar("Erro 2");

        var resultado = Assert.IsType<ObjectResult>(_controller.ChamarResponder(_notificacao, 200, "Ok"));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal(2, LerJson(resultado).GetProperty("erros").GetArrayLength());
    }

    [Fact]
    public void ResponderSucessoSemDados()
    {
        var resultado = Assert.IsType<ObjectResult>(_controller.ChamarResponder(_notificacao, 200, "Ok"));

        Assert.Equal(200, resultado.StatusCode);
        Assert.False(LerJson(resultado).TryGetProperty("dados", out _));
    }

    [Fact]
    public void ResponderNaoEncontrado()
    {
        _notificacao.AdicionarNaoEncontrado("Conta não encontrada.");

        var resultado = Assert.IsType<ObjectResult>(_controller.ChamarResponder(_notificacao, 200, "Ok"));

        Assert.Equal(404, resultado.StatusCode);
    }

    [Fact]
    public void ResponderConflito()
    {
        _notificacao.AdicionarConflito("Já existe uma conta com esse nome.");

        var resultado = Assert.IsType<ObjectResult>(_controller.ChamarResponder(_notificacao, 200, "Ok"));

        Assert.Equal(409, resultado.StatusCode);
    }

    [Fact]
    public void ResponderSemAvisos()
    {
        var resultado = Assert.IsType<ObjectResult>(_controller.ChamarResponder(_notificacao, 200, "Ok"));

        Assert.False(LerJson(resultado).TryGetProperty("avisos", out _));
    }
}
