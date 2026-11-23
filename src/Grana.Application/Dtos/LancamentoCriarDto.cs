namespace Application.Dtos;

public sealed record LancamentoCriarDto(Guid contaId, Guid categoriaId, decimal valor, DateOnly data, string descricao);
