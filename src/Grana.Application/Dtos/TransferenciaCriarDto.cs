namespace Application.Dtos;

public sealed record TransferenciaCriarDto(Guid contaOrigemId, Guid contaDestinoId, decimal valor, DateOnly data, string descricao);
