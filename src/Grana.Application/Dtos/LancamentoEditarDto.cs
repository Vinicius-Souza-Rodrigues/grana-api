namespace Application.Dtos;

public sealed record LancamentoEditarDto(Guid categoriaId, decimal valor, DateOnly data, string descricao);
