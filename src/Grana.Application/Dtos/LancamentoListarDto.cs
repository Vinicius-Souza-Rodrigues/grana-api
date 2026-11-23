namespace Application.Dtos;

public sealed record LancamentoListarDto(Guid? contaId, Guid? categoriaId, string tipo, DateOnly? dataInicio, DateOnly? dataFim, string busca, int? pagina, int? tamanhoPagina);
