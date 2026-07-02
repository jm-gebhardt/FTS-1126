using Gebhardt.Acc.PrismatCommunication.Domain.Models.Housekeeping;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Gebhardt.Acc.PrismatCommunication.Services.BackgroundServices.Housekeeping;

/// <summary>
/// Quartz job that executes housekeeping for one entity type.
/// The entity type is passed via the job data map using <see cref="EntityTypeKey"/>.
/// </summary>
[DisallowConcurrentExecution]
public class HousekeepingJob(IServiceProvider serviceProvider, ILogger<HousekeepingJob> logger) : IJob
{
    /// <summary>Job data map key that holds the <see cref="HousekeepingEntityType"/> value.</summary>
    public const string EntityTypeKey = "EntityType";

    /// <inheritdoc/>
    public async Task Execute(IJobExecutionContext context)
    {
        var entityType = (HousekeepingEntityType)context.JobDetail.JobDataMap.GetInt(EntityTypeKey);

        using var scope = serviceProvider.CreateScope();
        var configRepo = scope.ServiceProvider.GetRequiredService<IHousekeepingConfigurationRepository>();
        var config = await configRepo.GetConfigurationAsync(entityType);

        if (config == null)
        {
            logger.LogWarning("No housekeeping configuration found for {EntityType}, skipping", entityType);
            return;
        }

        var action = scope.ServiceProvider
            .GetRequiredService<IEnumerable<IHousekeepingAction>>()
            .FirstOrDefault(a => a.EntityType == entityType);

        if (action == null)
        {
            logger.LogWarning("No housekeeping action registered for {EntityType}, skipping", entityType);
            return;
        }

        logger.LogInformation("Starting housekeeping for {EntityType}, deleting entries older than {OutdatedTime}",
            entityType, config.GetOutdatedTime());

        await action.ExecuteAsync(config.GetOutdatedTime(), context.CancellationToken);

        logger.LogInformation("Housekeeping for {EntityType} completed", entityType);
    }
}
