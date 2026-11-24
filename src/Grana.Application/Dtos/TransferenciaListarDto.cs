namespace Application.Dtos;

public sealed record TransferenciaListarDto(Guid? contaId, DateOnly? dataInicio, DateOnly? dataFim);
