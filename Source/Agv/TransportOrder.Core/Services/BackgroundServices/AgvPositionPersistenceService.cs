using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

/// <summary>
/// Persists all in-memory AGV positions to the database on graceful shutdown.
/// </summary>
public class AgvPositionPersistenceService(IServiceScopeFactory scopeFactory, ILogger<AgvPositionPersistenceService> logger) : IHostedService
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var positionRepository = scope.ServiceProvider.GetRequiredService<IAgvPositionRepository>();
            int positionCount = positionRepository.GetAllPositions().Count;
            logger.LogInformation("Persisting {Count} AGV positions on shutdown", positionCount);
            await positionRepository.PersistAllPositionsAsync(cancellationToken);
            logger.LogInformation("Persisted all AGV positions on shutdown");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist AGV positions on shutdown");
        }
    }
}
