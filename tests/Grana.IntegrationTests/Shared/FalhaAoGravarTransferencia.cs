using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IntegrationTests.Shared;

public sealed class FalhaAoGravarTransferencia : DbCommandInterceptor
{
    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        Falhar(command);

        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Falhar(command);

        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    private static void Falhar(DbCommand command)
    {
        if (command.CommandText.Contains("INSERT INTO transferencias") && command.CommandText.Contains("UPDATE contas"))
            throw new InvalidOperationException("Falha simulada ao gravar a transferência.");
    }
}
