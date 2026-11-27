using Api.Controllers;

namespace Grana.Api.OpenApi;

public sealed record ExemploEndpoint(object Requisicao, IReadOnlyDictionary<int, object> Respostas);

public static class ExemplosOpenApi
{
    private const string MensagemErro = "Não foi possível concluir a operação.";
    private const string ConflitoConcorrencia = "A conta foi alterada por outra operação. Tente novamente.";

    private static readonly Guid IdNubank = Guid.Parse("3f2b8c1e-6a4d-4e1b-9c7a-2d5e8f1a4b6c");
    private static readonly Guid IdInter = Guid.Parse("7a1c9e4b-2f6d-4b8a-a3e5-9c1d7f2b5e8a");
    private static readonly Guid IdMercado = Guid.Parse("c4e7a2b9-8d1f-4a6c-b5e3-1f9a7c2d4e6b");
    private static readonly Guid IdSalario = Guid.Parse("e9b3d6a1-4c7f-4e2a-8b5d-6a1c3f9e7b2d");
    private static readonly Guid IdLancamento = Guid.Parse("1d8f4a7c-3e9b-4c2d-a6f1-8b4e2c7a9d5f");
    private static readonly Guid IdTransferencia = Guid.Parse("5b2e9c4a-7d1f-4a8b-9e3c-2f6a8d1b4c7e");
    private static readonly Guid IdUsuario = Guid.Parse("8c3f1a6d-9b2e-4d7a-b1c5-4e8a2f6d9c3b");

    private const string Data = "2026-11-20";
    private const string CriadoEm = "2026-11-20T14:32:10Z";

    private static readonly object ContaNubank = new { id = IdNubank, nome = "Nubank" };
    private static readonly object ContaInter = new { id = IdInter, nome = "Inter" };
    private static readonly object CategoriaMercado = new { id = IdMercado, nome = "Mercado" };
    private static readonly object CategoriaSalario = new { id = IdSalario, nome = "Salário" };

    private static readonly object ContaDetalhe = new { id = IdNubank, nome = "Nubank", tipo = "Corrente", saldo = 1500.00m, ativa = true, saldoInicial = 1500.00m, criadoEm = CriadoEm };
    private static readonly object CategoriaDetalhe = new { id = IdMercado, nome = "Mercado", tipo = "Despesa", orcamentoMensal = 800.00m };

    private static readonly object LancamentoDetalhe = new
    {
        id = IdLancamento,
        contaId = IdNubank,
        categoriaId = IdMercado,
        tipo = "Despesa",
        valor = 250.00m,
        data = Data,
        descricao = "Feira do mês",
        criadoEm = CriadoEm
    };

    private static readonly object TransferenciaDetalhe = new
    {
        id = IdTransferencia,
        data = Data,
        valor = 300.00m,
        descricao = "Reserva de emergência",
        contaOrigem = ContaNubank,
        contaDestino = ContaInter,
        criadoEm = CriadoEm
    };

    private static readonly object RequisicaoLancamento = new { contaId = IdNubank, categoriaId = IdMercado, valor = 250.00m, data = Data, descricao = "Feira do mês" };
    private static readonly object RequisicaoEditarLancamento = new { categoriaId = IdMercado, valor = 400.00m, data = Data, descricao = "Feira do mês" };
    private static readonly object RequisicaoTransferencia = new { contaOrigemId = IdNubank, contaDestinoId = IdInter, valor = 300.00m, data = Data, descricao = "Reserva de emergência" };

    public static readonly IReadOnlyDictionary<string, ExemploEndpoint> PorAction = new Dictionary<string, ExemploEndpoint>
    {
        [nameof(ContaController.CriarConta)] = new(
            new { nome = "Nubank", tipo = "Corrente", saldoInicial = 1500.00m },
            new Dictionary<int, object>
            {
                [201] = Sucesso(201, "Conta criada com sucesso.", ContaDetalhe),
                [400] = Erro(400, "O saldo inicial não pode ser menor que 0 ou nulo", "O tipo conta passado é invalido"),
                [409] = Erro(409, "Já existe uma conta ativa com esse nome.")
            }),

        [nameof(ContaController.ListarContas)] = new(
            null,
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Contas encontradas.", new object[]
                {
                    new { id = IdNubank, nome = "Nubank", tipo = "Corrente", saldo = 1500.00m, ativa = true },
                    new { id = IdInter, nome = "Inter", tipo = "Poupanca", saldo = 300.00m, ativa = true }
                })
            }),

        [nameof(ContaController.ObterContaPorId)] = new(
            null,
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Conta encontrada.", ContaDetalhe),
                [404] = Erro(404, "Conta não encontrada.")
            }),

        [nameof(ContaController.RenomearConta)] = new(
            new { nome = "Nubank Pessoal" },
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Conta atualizada com sucesso.", new { id = IdNubank, nome = "Nubank Pessoal", tipo = "Corrente", saldo = 1500.00m, ativa = true, saldoInicial = 1500.00m, criadoEm = CriadoEm }),
                [400] = Erro(400, "Não é possível editar uma conta desativada."),
                [404] = Erro(404, "Conta não encontrada."),
                [409] = Erro(409, "Já existe uma conta ativa com esse nome.")
            }),

        [nameof(ContaController.DesativarConta)] = new(
            null,
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Conta desativada com sucesso.", new { id = IdNubank, nome = "Nubank", tipo = "Corrente", saldo = 0.00m, ativa = false, saldoInicial = 1500.00m, criadoEm = CriadoEm }),
                [400] = Erro(400, "Só é possível desativar uma conta com saldo zero."),
                [404] = Erro(404, "Conta não encontrada."),
                [409] = Erro(409, ConflitoConcorrencia)
            }),

        [nameof(ContaController.ReativarConta)] = new(
            null,
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Conta reativada com sucesso.", ContaDetalhe),
                [400] = Erro(400, "Não se pode ativar uma conta já ativa"),
                [404] = Erro(404, "Conta não encontrada."),
                [409] = Erro(409, "Já existe uma conta ativa com esse nome.")
            }),

        [nameof(CategoriaController.CriarCategoria)] = new(
            new { nome = "Mercado", tipo = "Despesa", orcamentoMensal = 800.00m },
            new Dictionary<int, object>
            {
                [201] = Sucesso(201, "Categoria criada com sucesso.", CategoriaDetalhe),
                [400] = Erro(400, "Orçamento só pode ser definido para categorias de despesa."),
                [409] = Erro(409, "Já existe uma categoria com esse nome para esse tipo.")
            }),

        [nameof(CategoriaController.ListarCategorias)] = new(
            null,
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Categorias encontradas.", new object[]
                {
                    CategoriaDetalhe,
                    new { id = IdSalario, nome = "Salário", tipo = "Receita" }
                }),
                [400] = Erro(400, "O tipo da categoria passado é inválido.")
            }),

        [nameof(CategoriaController.ObterCategoriaPorId)] = new(
            null,
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Categoria encontrada.", CategoriaDetalhe),
                [404] = Erro(404, "Categoria não encontrada.")
            }),

        [nameof(CategoriaController.EditarCategoria)] = new(
            new { nome = "Supermercado", orcamentoMensal = 900.00m },
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Categoria atualizada com sucesso.", new { id = IdMercado, nome = "Supermercado", tipo = "Despesa", orcamentoMensal = 900.00m }),
                [400] = Erro(400, "O orçamento mensal deve ser maior que zero."),
                [404] = Erro(404, "Categoria não encontrada."),
                [409] = Erro(409, "Já existe uma categoria com esse nome para esse tipo.")
            }),

        [nameof(CategoriaController.ExcluirCategoria)] = new(
            null,
            new Dictionary<int, object>
            {
                [400] = Erro(400, "Não é possível excluir uma categoria com lançamentos."),
                [404] = Erro(404, "Categoria não encontrada.")
            }),

        [nameof(LancamentoController.CriarLancamento)] = new(
            RequisicaoLancamento,
            new Dictionary<int, object>
            {
                [201] = Sucesso(201, "Lançamento criado com sucesso.", new { lancamento = LancamentoDetalhe, saldoConta = 1250.00m }, "A categoria Mercado passou de 80% do orçamento."),
                [400] = Erro(400, "Saldo insuficiente.", "A data do lançamento não pode ser futura."),
                [404] = Erro(404, "Conta não encontrada.", "Categoria não encontrada."),
                [409] = Erro(409, ConflitoConcorrencia)
            }),

        [nameof(LancamentoController.EditarLancamento)] = new(
            RequisicaoEditarLancamento,
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Lançamento atualizado com sucesso.", new
                {
                    lancamento = new { id = IdLancamento, contaId = IdNubank, categoriaId = IdMercado, tipo = "Despesa", valor = 400.00m, data = Data, descricao = "Feira do mês", criadoEm = CriadoEm },
                    saldoConta = 1100.00m
                }),
                [400] = Erro(400, "A nova categoria precisa ser do mesmo tipo do lançamento."),
                [404] = Erro(404, "Lançamento não encontrado."),
                [409] = Erro(409, ConflitoConcorrencia)
            }),

        [nameof(LancamentoController.ExcluirLancamento)] = new(
            null,
            new Dictionary<int, object>
            {
                [400] = Erro(400, "Saldo insuficiente."),
                [404] = Erro(404, "Lançamento não encontrado."),
                [409] = Erro(409, ConflitoConcorrencia)
            }),

        [nameof(LancamentoController.ListarLancamentos)] = new(
            null,
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Lançamentos encontrados.", new
                {
                    itens = new object[]
                    {
                        new { id = IdLancamento, data = Data, descricao = "Feira do mês", valor = 250.00m, tipo = "Despesa", conta = ContaNubank, categoria = CategoriaMercado }
                    },
                    pagina = 1,
                    tamanhoPagina = 20,
                    totalItens = 1,
                    totalPaginas = 1
                }),
                [400] = Erro(400, "A data de início não pode ser maior que a data de fim.", "O tamanho da página deve estar entre 1 e 100.")
            }),

        [nameof(LancamentoController.ObterLancamentoPorId)] = new(
            null,
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Lançamento encontrado.", LancamentoDetalhe),
                [404] = Erro(404, "Lançamento não encontrado.")
            }),

        [nameof(TransferenciaController.CriarTransferencia)] = new(
            RequisicaoTransferencia,
            new Dictionary<int, object>
            {
                [201] = Sucesso(201, "Transferência realizada com sucesso.", new { transferencia = TransferenciaDetalhe, saldoContaOrigem = 1200.00m, saldoContaDestino = 600.00m }),
                [400] = Erro(400, "Saldo insuficiente.", "A conta de destino está desativada."),
                [404] = Erro(404, "Conta de origem não encontrada."),
                [409] = Erro(409, ConflitoConcorrencia)
            }),

        [nameof(TransferenciaController.ListarTransferencias)] = new(
            null,
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Transferências encontradas.", new object[]
                {
                    new { id = IdTransferencia, data = Data, valor = 300.00m, descricao = "Reserva de emergência", contaOrigem = ContaNubank, contaDestino = ContaInter }
                }),
                [400] = Erro(400, "A data de início não pode ser maior que a data de fim.")
            }),

        [nameof(TransferenciaController.ObterTransferenciaPorId)] = new(
            null,
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Transferência encontrada.", TransferenciaDetalhe),
                [404] = Erro(404, "Transferência não encontrada.")
            }),

        [nameof(TransferenciaController.ExcluirTransferencia)] = new(
            null,
            new Dictionary<int, object>
            {
                [400] = Erro(400, "Saldo insuficiente."),
                [404] = Erro(404, "Transferência não encontrada."),
                [409] = Erro(409, ConflitoConcorrencia)
            }),

        [nameof(OrcamentoController.ListarOrcamentos)] = new(
            null,
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Orçamentos encontrados.", new
                {
                    ano = 2026,
                    mes = 11,
                    totalOrcado = 800.00m,
                    totalGasto = 680.00m,
                    categorias = new object[]
                    {
                        new { categoria = CategoriaMercado, limite = 800.00m, gasto = 680.00m, disponivel = 120.00m, percentualUsado = 85.0m, situacao = "Alerta" }
                    }
                }),
                [400] = Erro(400, "O mês deve estar entre 1 e 12.")
            }),

        [nameof(AuthController.Registrar)] = new(
            new { nome = "Maria Silva", email = "maria@email.com", senha = "senhaForte123" },
            new Dictionary<int, object>
            {
                [201] = Sucesso(201, "Usuário registrado com sucesso.", new { id = IdUsuario, nome = "Maria Silva", email = "maria@email.com", criadoEm = CriadoEm }),
                [400] = Erro(400, "O nome passado é inválido, não pode ser nulo.", "A senha deve ter no mínimo 8 caracteres, com pelo menos uma letra e um número."),
                [409] = Erro(409, "Já existe um usuário com esse e-mail.")
            }),

        [nameof(ResumoController.ObterResumoMensal)] = new(
            null,
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Resumo do mês encontrado.", new
                {
                    ano = 2026,
                    mes = 11,
                    totalReceitas = 5000.00m,
                    totalDespesas = 680.00m,
                    resultadoDoMes = 4320.00m,
                    saldoAtualContas = 5820.00m,
                    comparativoMesAnterior = new { receitas = 0.0m, despesas = -12.5m },
                    despesasPorCategoria = new object[] { new { categoria = CategoriaMercado, total = 680.00m, percentual = 100.0m } },
                    receitasPorCategoria = new object[] { new { categoria = CategoriaSalario, total = 5000.00m, percentual = 100.0m } }
                }),
                [400] = Erro(400, "O mês deve estar entre 1 e 12.")
            }),

        [nameof(ExtratoController.ObterExtratoConta)] = new(
            null,
            new Dictionary<int, object>
            {
                [200] = Sucesso(200, "Extrato encontrado.", new
                {
                    conta = ContaNubank,
                    dataInicio = "2026-11-01",
                    dataFim = "2026-11-30",
                    saldoAnterior = 1000.00m,
                    totalEntradas = 5000.00m,
                    totalSaidas = 550.00m,
                    saldoFinal = 5450.00m,
                    movimentacoes = new object[]
                    {
                        new { id = IdSalario, tipo = "Receita", data = "2026-11-05", descricao = "Salário", valor = 5000.00m, saldoApos = 6000.00m },
                        new { id = IdLancamento, tipo = "Despesa", data = Data, descricao = "Feira do mês", valor = -250.00m, saldoApos = 5750.00m },
                        new { id = IdTransferencia, tipo = "TransferenciaEnviada", data = Data, descricao = "Reserva de emergência", valor = -300.00m, saldoApos = 5450.00m }
                    }
                }),
                [400] = Erro(400, "O período do extrato não pode ter mais que 366 dias."),
                [404] = Erro(404, "Conta não encontrada.")
            })
    };

    private static object Sucesso(int codigo, string mensagem, object dados)
        => new { codigo, mensagem, dados };

    private static object Sucesso(int codigo, string mensagem, object dados, params string[] avisos)
        => new { codigo, mensagem, dados, avisos };

    private static object Erro(int codigo, params string[] erros)
        => new { codigo, mensagem = MensagemErro, erros };
}
