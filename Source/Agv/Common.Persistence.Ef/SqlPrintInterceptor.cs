using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;

namespace Gebhardt.Acc.Common.Persistence.Ef;

/// <summary>
/// EF Core connection interceptor that forwards SQL PRINT output to the .NET logger.
/// </summary>
public class SqlPrintInterceptor(ILogger<SqlPrintInterceptor> logger) : DbConnectionInterceptor
{
    /// <inheritdoc />
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        => Subscribe(connection);

    /// <inheritdoc />
    public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Subscribe(connection);
        return Task.CompletedTask;
    }

    private void Subscribe(DbConnection connection)
    {
        if (connection is SqlConnection sqlConnection)
        {
            sqlConnection.InfoMessage += OnInfoMessage;
        }
    }

    private void OnInfoMessage(object sender, SqlInfoMessageEventArgs e)
        => logger.LogInformation("[SQL] {Message}", e.Message);
}
