using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Controllers;
using Application.Services;
using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;
using Infraestructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Tests.Api;

public sealed class LancamentoControllerTest
{
    private static readonly JsonSerializerOptions _opcoesJson = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly LancamentoRepository _lancamentoRepositorio = new();
    private readonly LancamentoController _controller;

    public LancamentoControllerTest()
    {
        _controller = new LancamentoController(new LancamentoService(_lancamentoRepositorio));
    }

    private static JsonElement LerJson(ObjectResult resultado)
        => JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value, _opcoesJson)).RootElement;

    [Fact]
    public async Task ObterLancamentoExistente()
    {
        var idConta = Guid.NewGuid();
        var categoria = Categoria.CriacaoCategoria("Mercado", TipoCategoriaEnum.Despesa, null, new Notificacao());
        var lancamento = Lancamento.CriacaoLancamento(idConta, categoria, 150.50m, DateOnly.FromDateTime(DateTime.UtcNow), "Compra do mês", new Notificacao());
        await _lancamentoRepositorio.AdicionarLancamento(lancamento, CancellationToken.None);

        var resultado = Assert.IsType<ObjectResult>(await _controller.ObterLancamentoPorId(lancamento.Id, CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(lancamento.Id, dados.GetProperty("id").GetGuid());
        Assert.Equal(idConta, dados.GetProperty("contaId").GetGuid());
        Assert.Equal(categoria.Id, dados.GetProperty("categoriaId").GetGuid());
        Assert.Equal(150.50m, dados.GetProperty("valor").GetDecimal());
        Assert.Equal("Compra do mês", dados.GetProperty("descricao").GetString());
    }

    [Fact]
    public async Task ObterLancamentoInexistente()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ObterLancamentoPorId(Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
        Assert.Equal("Lançamento não encontrado.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }
}
