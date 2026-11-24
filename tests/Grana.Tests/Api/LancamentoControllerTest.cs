using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Controllers;
using Application.Dtos;
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
    private readonly ContaRepository _contaRepositorio = new();
    private readonly CategoriaRepository _categoriaRepositorio = new();
    private readonly LancamentoController _controller;

    public LancamentoControllerTest()
    {
        _controller = new LancamentoController(new LancamentoService(_lancamentoRepositorio, _contaRepositorio, _categoriaRepositorio));
    }

    private static DateOnly Hoje => DateOnly.FromDateTime(DateTime.UtcNow);

    private static JsonElement LerJson(ObjectResult resultado)
        => JsonDocument.Parse(JsonSerializer.Serialize(resultado.Value, _opcoesJson)).RootElement;

    [Fact]
    public async Task CriarLancamentoDeReceita()
    {
        var conta = await CriarConta(100m);
        var categoria = await CriarCategoria("Salário", TipoCategoriaEnum.Receita, null);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 50m, Hoje, "Freela"), CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        Assert.Equal(201, resultado.StatusCode);
        Assert.Equal(nameof(LancamentoController.ObterLancamentoPorId), resultado.ActionName);
        Assert.Equal(150m, dados.GetProperty("saldoConta").GetDecimal());
        Assert.Equal(50m, dados.GetProperty("lancamento").GetProperty("valor").GetDecimal());
        Assert.Equal(150m, conta.Saldo);
    }

    [Fact]
    public async Task CriarLancamentoDeDespesaZerandoOSaldo()
    {
        var conta = await CriarConta(100m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 100m, Hoje, null), CancellationToken.None));

        Assert.Equal(201, resultado.StatusCode);
        Assert.Equal(0m, LerJson(resultado).GetProperty("dados").GetProperty("saldoConta").GetDecimal());
    }

    [Fact]
    public async Task CriarLancamentoDeDespesaComSaldoInsuficiente()
    {
        var conta = await CriarConta(100m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 100.01m, Hoje, null), CancellationToken.None));

        var erros = LerJson(resultado).GetProperty("erros");
        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal(1, erros.GetArrayLength());
        Assert.Equal("Saldo insuficiente.", erros[0].GetString());
        Assert.Equal(100m, conta.Saldo);
        Assert.False(await _lancamentoRepositorio.ExisteLancamentoComCategoria(categoria.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CriarLancamentoComContaDesativadaValorZeroEDataFutura()
    {
        var conta = await CriarConta(0m);
        conta.Desativar(new Notificacao());
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 0m, Hoje.AddDays(1), null), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal(3, LerJson(resultado).GetProperty("erros").GetArrayLength());
    }

    [Fact]
    public async Task CriarLancamentoQueUltrapassaOOrcamento()
    {
        var conta = await CriarConta(2000m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, 800m);
        await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 700m, Hoje, null), CancellationToken.None);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 150m, Hoje, null), CancellationToken.None));

        var avisos = LerJson(resultado).GetProperty("avisos");
        Assert.Equal(201, resultado.StatusCode);
        Assert.Equal(1, avisos.GetArrayLength());
        Assert.Equal("O orçamento da categoria Mercado foi ultrapassado.", avisos[0].GetString());
        Assert.Equal(1150m, conta.Saldo);
    }

    [Fact]
    public async Task CriarLancamentoQuePassaDe80PorcentoDoOrcamento()
    {
        var conta = await CriarConta(2000m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, 800m);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 700m, Hoje, null), CancellationToken.None));

        Assert.Equal("A categoria Mercado passou de 80% do orçamento.", LerJson(resultado).GetProperty("avisos")[0].GetString());
    }

    [Fact]
    public async Task CriarLancamentoSemAvisoQuandoAbaixoDe80Porcento()
    {
        var conta = await CriarConta(2000m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, 800m);

        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, 100m, Hoje, null), CancellationToken.None));

        Assert.False(LerJson(resultado).TryGetProperty("avisos", out _));
    }

    [Fact]
    public async Task CriarLancamentoComContaInexistente()
    {
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarLancamento(new LancamentoCriarDto(Guid.NewGuid(), categoria.Id, 10m, Hoje, null), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
        Assert.Equal("Conta não encontrada.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    [Fact]
    public async Task CriarLancamentoComCategoriaInexistente()
    {
        var conta = await CriarConta(100m);

        var resultado = Assert.IsType<ObjectResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, Guid.NewGuid(), 10m, Hoje, null), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
        Assert.Equal("Categoria não encontrada.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    [Fact]
    public async Task ObterLancamentoExistente()
    {
        var conta = await CriarConta(100m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);
        var lancamento = await AdicionarLancamento(conta, categoria, 150.50m, Hoje, "Compra do mês");

        var resultado = Assert.IsType<ObjectResult>(await _controller.ObterLancamentoPorId(lancamento.Id, CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(lancamento.Id, dados.GetProperty("id").GetGuid());
        Assert.Equal(conta.Id, dados.GetProperty("contaId").GetGuid());
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

    [Fact]
    public async Task ListarLancamentosTerceiraPaginaCom57Lancamentos()
    {
        var conta = await CriarConta(100m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);

        for (var i = 0; i < 57; i++)
            await AdicionarLancamento(conta, categoria, 10m, Hoje, null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarLancamentos(Filtro(pagina: 3, tamanhoPagina: 20), CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(17, dados.GetProperty("itens").GetArrayLength());
        Assert.Equal(3, dados.GetProperty("pagina").GetInt32());
        Assert.Equal(20, dados.GetProperty("tamanhoPagina").GetInt32());
        Assert.Equal(57, dados.GetProperty("totalItens").GetInt32());
        Assert.Equal(3, dados.GetProperty("totalPaginas").GetInt32());
    }

    [Fact]
    public async Task ListarLancamentosComDataInicioMaiorQueDataFim()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarLancamentos(Filtro(dataInicio: Hoje, dataFim: Hoje.AddDays(-1)), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("A data de início não pode ser maior que a data de fim.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    [Fact]
    public async Task ListarLancamentosComTamanhoPagina500()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarLancamentos(Filtro(tamanhoPagina: 500), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
    }

    [Fact]
    public async Task ListarLancamentosComTipoInvalido()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarLancamentos(Filtro(tipo: "Investimento"), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
    }

    [Fact]
    public async Task ListarLancamentosComBuscaSemDiferenciarMaiusculas()
    {
        var conta = await CriarConta(100m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);
        await AdicionarLancamento(conta, categoria, 10m, Hoje, "Supermercado");
        await AdicionarLancamento(conta, categoria, 10m, Hoje, "Padaria");
        await AdicionarLancamento(conta, categoria, 10m, Hoje, null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarLancamentos(Filtro(busca: "mercado"), CancellationToken.None));

        var itens = LerJson(resultado).GetProperty("dados").GetProperty("itens");
        Assert.Equal(1, itens.GetArrayLength());
        Assert.Equal("Supermercado", itens[0].GetProperty("descricao").GetString());
    }

    [Fact]
    public async Task ListarLancamentosOrdenadosPorDataDecrescente()
    {
        var conta = await CriarConta(100m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);
        await AdicionarLancamento(conta, categoria, 10m, Hoje.AddDays(-2), "Antigo");
        await AdicionarLancamento(conta, categoria, 10m, Hoje, "Recente");
        await AdicionarLancamento(conta, categoria, 10m, Hoje.AddDays(-1), "Meio");

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarLancamentos(Filtro(), CancellationToken.None));

        var itens = LerJson(resultado).GetProperty("dados").GetProperty("itens");
        Assert.Equal("Recente", itens[0].GetProperty("descricao").GetString());
        Assert.Equal("Meio", itens[1].GetProperty("descricao").GetString());
        Assert.Equal("Antigo", itens[2].GetProperty("descricao").GetString());
    }

    [Fact]
    public async Task ListarLancamentosFiltrandoPorContaTipoEPeriodo()
    {
        var nubank = await CriarConta(100m);
        var inter = await CriarConta(100m, "Inter");
        var mercado = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);
        var salario = await CriarCategoria("Salário", TipoCategoriaEnum.Receita, null);
        await AdicionarLancamento(nubank, mercado, 10m, Hoje.AddDays(-5), "Fora do período");
        await AdicionarLancamento(nubank, mercado, 20m, Hoje.AddDays(-2), "Esperado");
        await AdicionarLancamento(nubank, salario, 30m, Hoje.AddDays(-2), "Outro tipo");
        await AdicionarLancamento(inter, mercado, 40m, Hoje.AddDays(-2), "Outra conta");

        var filtro = Filtro(contaId: nubank.Id, tipo: "despesa", dataInicio: Hoje.AddDays(-3), dataFim: Hoje.AddDays(-2));
        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarLancamentos(filtro, CancellationToken.None));

        var itens = LerJson(resultado).GetProperty("dados").GetProperty("itens");
        Assert.Equal(1, itens.GetArrayLength());
        Assert.Equal("Esperado", itens[0].GetProperty("descricao").GetString());
        Assert.Equal("Nubank", itens[0].GetProperty("conta").GetProperty("nome").GetString());
        Assert.Equal(mercado.Id, itens[0].GetProperty("categoria").GetProperty("id").GetGuid());
        Assert.Equal("Mercado", itens[0].GetProperty("categoria").GetProperty("nome").GetString());
    }

    [Fact]
    public async Task ListarLancamentosFiltrandoPorCategoria()
    {
        var conta = await CriarConta(100m);
        var mercado = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);
        var transporte = await CriarCategoria("Transporte", TipoCategoriaEnum.Despesa, null);
        await AdicionarLancamento(conta, mercado, 10m, Hoje, "Compra");
        await AdicionarLancamento(conta, transporte, 10m, Hoje, "Uber");

        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarLancamentos(Filtro(categoriaId: transporte.Id), CancellationToken.None));

        var itens = LerJson(resultado).GetProperty("dados").GetProperty("itens");
        Assert.Equal(1, itens.GetArrayLength());
        Assert.Equal("Uber", itens[0].GetProperty("descricao").GetString());
    }

    [Fact]
    public async Task ListarLancamentosSemParametrosUsaPaginacaoPadrao()
    {
        var resultado = Assert.IsType<ObjectResult>(await _controller.ListarLancamentos(Filtro(), CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        Assert.Equal(1, dados.GetProperty("pagina").GetInt32());
        Assert.Equal(20, dados.GetProperty("tamanhoPagina").GetInt32());
        Assert.Equal(0, dados.GetProperty("itens").GetArrayLength());
    }

    [Fact]
    public async Task EditarDespesaAumentandoOValor()
    {
        var conta = await CriarConta(250m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);
        var id = await CriarLancamentoPeloEndpoint(conta, categoria, 50m);
        Assert.Equal(200m, conta.Saldo);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarLancamento(id, new LancamentoEditarDto(categoria.Id, 80m, Hoje, "Compra maior"), CancellationToken.None));

        var dados = LerJson(resultado).GetProperty("dados");
        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(170m, dados.GetProperty("saldoConta").GetDecimal());
        Assert.Equal(80m, dados.GetProperty("lancamento").GetProperty("valor").GetDecimal());
        Assert.Equal("Compra maior", dados.GetProperty("lancamento").GetProperty("descricao").GetString());
        Assert.Equal(170m, conta.Saldo);
    }

    [Fact]
    public async Task EditarDespesaDeixandoSaldoNegativo()
    {
        var conta = await CriarConta(60m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);
        var id = await CriarLancamentoPeloEndpoint(conta, categoria, 50m);
        Assert.Equal(10m, conta.Saldo);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarLancamento(id, new LancamentoEditarDto(categoria.Id, 100m, Hoje, "Alterado"), CancellationToken.None));

        var lancamento = await _lancamentoRepositorio.ObterLancamentoPorId(id, CancellationToken.None);
        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("Saldo insuficiente.", LerJson(resultado).GetProperty("erros")[0].GetString());
        Assert.Equal(10m, conta.Saldo);
        Assert.Equal(50m, lancamento.Valor);
        Assert.Null(lancamento.Descricao);
    }

    [Fact]
    public async Task EditarDespesaParaCategoriaDeReceita()
    {
        var conta = await CriarConta(100m);
        var mercado = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);
        var salario = await CriarCategoria("Salário", TipoCategoriaEnum.Receita, null);
        var id = await CriarLancamentoPeloEndpoint(conta, mercado, 50m);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarLancamento(id, new LancamentoEditarDto(salario.Id, 50m, Hoje, null), CancellationToken.None));

        var lancamento = await _lancamentoRepositorio.ObterLancamentoPorId(id, CancellationToken.None);
        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal("A nova categoria precisa ser do mesmo tipo do lançamento.", LerJson(resultado).GetProperty("erros")[0].GetString());
        Assert.Equal(mercado.Id, lancamento.IdCategoria);
        Assert.Equal(50m, conta.Saldo);
    }

    [Fact]
    public async Task EditarReceitaDiminuindoOValor()
    {
        var conta = await CriarConta(100m);
        var categoria = await CriarCategoria("Salário", TipoCategoriaEnum.Receita, null);
        var id = await CriarLancamentoPeloEndpoint(conta, categoria, 500m);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarLancamento(id, new LancamentoEditarDto(categoria.Id, 300m, Hoje, null), CancellationToken.None));

        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(400m, conta.Saldo);
    }

    [Fact]
    public async Task EditarLancamentoTrocandoParaCategoriaDoMesmoTipo()
    {
        var conta = await CriarConta(100m);
        var mercado = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);
        var transporte = await CriarCategoria("Transporte", TipoCategoriaEnum.Despesa, null);
        var id = await CriarLancamentoPeloEndpoint(conta, mercado, 50m);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarLancamento(id, new LancamentoEditarDto(transporte.Id, 50m, Hoje, null), CancellationToken.None));

        Assert.Equal(200, resultado.StatusCode);
        Assert.Equal(transporte.Id, LerJson(resultado).GetProperty("dados").GetProperty("lancamento").GetProperty("categoriaId").GetGuid());
        Assert.Equal(50m, conta.Saldo);
    }

    [Fact]
    public async Task EditarLancamentoComContaDesativada()
    {
        var conta = await CriarConta(100m);
        var categoria = await CriarCategoria("Salário", TipoCategoriaEnum.Receita, null);
        var id = await CriarLancamentoPeloEndpoint(conta, categoria, 50m);
        conta.Debitar(150m, new Notificacao());
        conta.Desativar(new Notificacao());

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarLancamento(id, new LancamentoEditarDto(categoria.Id, 80m, Hoje, null), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal(0m, conta.Saldo);
    }

    [Fact]
    public async Task EditarLancamentoComDadosInvalidos()
    {
        var conta = await CriarConta(100m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);
        var id = await CriarLancamentoPeloEndpoint(conta, categoria, 50m);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarLancamento(id, new LancamentoEditarDto(categoria.Id, 0m, Hoje.AddDays(1), null), CancellationToken.None));

        Assert.Equal(400, resultado.StatusCode);
        Assert.Equal(2, LerJson(resultado).GetProperty("erros").GetArrayLength());
        Assert.Equal(50m, conta.Saldo);
    }

    [Fact]
    public async Task EditarLancamentoInexistente()
    {
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarLancamento(Guid.NewGuid(), new LancamentoEditarDto(categoria.Id, 10m, Hoje, null), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
        Assert.Equal("Lançamento não encontrado.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    [Fact]
    public async Task EditarLancamentoComCategoriaInexistente()
    {
        var conta = await CriarConta(100m);
        var categoria = await CriarCategoria("Mercado", TipoCategoriaEnum.Despesa, null);
        var id = await CriarLancamentoPeloEndpoint(conta, categoria, 50m);

        var resultado = Assert.IsType<ObjectResult>(await _controller.EditarLancamento(id, new LancamentoEditarDto(Guid.NewGuid(), 10m, Hoje, null), CancellationToken.None));

        Assert.Equal(404, resultado.StatusCode);
        Assert.Equal("Categoria não encontrada.", LerJson(resultado).GetProperty("erros")[0].GetString());
    }

    private async Task<Guid> CriarLancamentoPeloEndpoint(Conta conta, Categoria categoria, decimal valor)
    {
        var resultado = Assert.IsType<CreatedAtActionResult>(await _controller.CriarLancamento(new LancamentoCriarDto(conta.Id, categoria.Id, valor, Hoje, null), CancellationToken.None));

        return (Guid)resultado.RouteValues["id"];
    }

    private static LancamentoListarDto Filtro(Guid? contaId = null, Guid? categoriaId = null, string tipo = null, DateOnly? dataInicio = null, DateOnly? dataFim = null, string busca = null, int? pagina = null, int? tamanhoPagina = null)
        => new(contaId, categoriaId, tipo, dataInicio, dataFim, busca, pagina, tamanhoPagina);

    private async Task<Lancamento> AdicionarLancamento(Conta conta, Categoria categoria, decimal valor, DateOnly data, string descricao)
    {
        var lancamento = Lancamento.CriacaoLancamento(conta, categoria, valor, data, descricao, new Notificacao());
        await _lancamentoRepositorio.AdicionarLancamento(lancamento, CancellationToken.None);

        return lancamento;
    }

    private async Task<Conta> CriarConta(decimal saldo, string nome = "Nubank")
    {
        var conta = Conta.CriacaoConta(nome, TipoContaEnum.Corrente, saldo, new Notificacao());
        await _contaRepositorio.AdicionarConta(conta, CancellationToken.None);

        return conta;
    }

    private async Task<Categoria> CriarCategoria(string nome, TipoCategoriaEnum tipo, decimal? orcamentoMensal)
    {
        var categoria = Categoria.CriacaoCategoria(nome, tipo, orcamentoMensal, new Notificacao());
        await _categoriaRepositorio.AdicionarCategoria(categoria, CancellationToken.None);

        return categoria;
    }
}
