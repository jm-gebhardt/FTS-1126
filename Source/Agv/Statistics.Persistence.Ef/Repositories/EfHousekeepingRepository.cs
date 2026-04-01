using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Persistence.Ef.DataAccess;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Persistence.Ef.Repositories;

/// <inheritdoc cref="IHouseKeepingRepository"/>>
public class EfHousekeepingRepository(StatisticsDbContext context, ILogger<EfHousekeepingRepository> logger) : IHouseKeepingRepository
{
    /// <inheritdoc cref="IHouseKeepingRepository"/>>
    public async Task CompressStatistics()
    {
        string? connectionString = context.Database.GetConnectionString();

        await using var connection = new SqlConnection(connectionString);

        connection.InfoMessage += (_, e) =>
        {
            logger.LogInformation("Compression stored procedure output: {Message}", e.Message);
        };

        await connection.OpenAsync();

        await using var command = new SqlCommand("EXEC [dbo].[CompressAgvStatistics]", connection);
        command.CommandTimeout = (int)TimeSpan.FromMinutes(30).TotalSeconds;
        await command.ExecuteNonQueryAsync();
    }
}