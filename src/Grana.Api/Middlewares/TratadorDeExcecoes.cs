using Microsoft.AspNetCore.Diagnostics;

namespace Grana.Api.Middlewares;

public sealed class TratadorDeExcecoes(ILogger<TratadorDeExcecoes> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Erro não tratado");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new { codigo = 500, mensagem = "Erro interno. Tente novamente mais tarde." }, cancellationToken);
        return true;
    }
}
