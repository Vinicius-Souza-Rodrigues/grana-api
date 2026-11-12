using Application.Dtos;
using Application.Interfaces;
using Domain.Enums;
using Domain.Models;
using Grana.Domain.Notificacoes;

namespace Application.Services;

public sealed class ContaService(IContaRepository contaRepository) : IContaService
{
    public async Task<Conta> CriarConta(ContaCriarDto dto, Notificacao notificacao, CancellationToken cancellationToken)
    {
        Enum.TryParse(dto.tipo, true, out TipoContaEnum tipoConta);

        var conta = Conta.CriacaoConta(dto.nome, tipoConta, dto.saldoInicial, notificacao);

        if (notificacao.TemNotificacoes)
            return null;

        if (await contaRepository.ExisteContaAtivaComNome(conta.Nome, cancellationToken))
        {
            notificacao.AdicionarConflito("Já existe uma conta ativa com esse nome.");
            return null;
        }

        await contaRepository.AdicionarConta(conta, cancellationToken);

        return conta;
    }

    public async Task<Conta> ObterContaPorId(Guid idConta, Notificacao notificacao, CancellationToken cancellationToken)
    {
        var conta = await contaRepository.ObterContaPorId(idConta, cancellationToken);

        if (conta is null)
            notificacao.AdicionarNaoEncontrado("Conta não encontrada.");

        return conta;
    }

    public async Task<Notificacao> DesativarConta(Guid idConta, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var conta = await contaRepository.ObterContaPorId(idConta, cancellationToken);

        if (conta is null)
        {
            notificacao.AdicionarNaoEncontrado("Conta não encontrada.");
            return notificacao;
        }

        conta.Desativar(notificacao);

        return notificacao;
    }

    public async Task<Notificacao> ReativarConta(Guid idConta, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var conta = await contaRepository.ObterContaPorId(idConta, cancellationToken);

        if (conta is null)
        {
            notificacao.AdicionarNaoEncontrado("Conta não encontrada.");
            return notificacao;
        }

        conta.Reativar(notificacao);

        return notificacao;
    }

    public async Task<Notificacao> CreditarConta(Guid idConta, decimal valorCredito, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var conta = await contaRepository.ObterContaPorId(idConta, cancellationToken);

        if (conta is null)
        {
            notificacao.AdicionarNaoEncontrado("Conta não encontrada.");
            return notificacao;
        }

        conta.Creditar(valorCredito, notificacao);

        return notificacao;
    }

    public async Task<Notificacao> DebitarConta(Guid idConta, decimal valorDebito, CancellationToken cancellationToken)
    {
        var notificacao = new Notificacao();

        var conta = await contaRepository.ObterContaPorId(idConta, cancellationToken);

        if (conta is null)
        {
            notificacao.AdicionarNaoEncontrado("Conta não encontrada.");
            return notificacao;
        }

        conta.Debitar(valorDebito, notificacao);

        return notificacao;
    }
}
