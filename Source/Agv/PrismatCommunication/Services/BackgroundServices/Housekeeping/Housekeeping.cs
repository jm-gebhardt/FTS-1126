using Gebhardt.Acc.PrismatCommunication.Domain.Models.Housekeeping;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Gebhardt.Acc.PrismatCommunication.Services.BackgroundServices.Housekeeping;

/// <summary>
/// Registers one Quartz housekeeping job per entity type and initialises their configurations on startup.
/// </summary>
public class Housekeeping(
    ISchedulerFactory schedulerFactory,
    ILogger<Housekeeping> logger,
    IServiceProvider serviceProvider,
    IConfiguration configuration) : BackgroundService
{
    private const string QuartzGroup = "DailyTasks";

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeConfigurationsAsync();

        var scheduler = await schedulerFactory.GetScheduler(stoppingToken);

        foreach (var entityType in Enum.GetValues<HousekeepingEntityType>())
        {
            using var scope = serviceProvider.CreateScope();
            var configRepo = scope.ServiceProvider.GetRequiredService<IHousekeepingConfigurationRepository>();
            var config = await configRepo.GetConfigurationAsync(entityType);

            if (config == null)
            {
                logger.LogWarning("No configuration available for {EntityType} housekeeping, skipping job registration", entityType);
                continue;
            }

            var jobKey = new JobKey($"HousekeepingJob_{entityType}", QuartzGroup);

            var job = JobBuilder.Create<HousekeepingJob>()
                .WithIdentity(jobKey)
                .UsingJobData(HousekeepingJob.EntityTypeKey, (int)entityType)
                .Build();

            var trigger = TriggerBuilder.Create()
                .WithIdentity($"HousekeepingTrigger_{entityType}", QuartzGroup)
                .StartNow()
                .WithSchedule(config.BuildSchedule())
                .ForJob(jobKey)
                .Build();

            await scheduler.ScheduleJob(job, trigger, stoppingToken);

            logger.LogInformation(
                "Scheduled housekeeping for {EntityType}: Every {ScheduleValue} {ScheduleInterval} at {Hour:D2}:{Minute:D2} (UTC), outdated after {OutdatedValue} {OutdatedInterval}",
                entityType, config.ScheduleValue, config.ScheduleInterval,
                config.ScheduleHour, config.ScheduleMinute,
                config.OutdatedValue, config.OutdatedInterval);
        }

        await scheduler.Start(stoppingToken);
    }

    private async Task InitializeConfigurationsAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var configRepo = scope.ServiceProvider.GetRequiredService<IHousekeepingConfigurationRepository>();

        foreach (var entityType in Enum.GetValues<HousekeepingEntityType>())
        {
            var existing = await configRepo.GetConfigurationAsync(entityType);
            if (existing == null)
            {
                var defaults = HousekeepingConfiguration.CreateDefault(entityType, configuration);
                await configRepo.AddOrUpdateAsync(defaults);
                logger.LogInformation("Initialized default housekeeping configuration for {EntityType}", entityType);
            }
        }
    }
}
