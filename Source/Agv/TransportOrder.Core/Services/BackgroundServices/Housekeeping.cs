using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;

/// <summary>
/// Provides a service for database housekeeping
/// </summary>
public class Housekeeping : BackgroundService, IJob
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Housekeeping> _logger;
    private readonly IHousekeepingConfigurationRepository _houseKeepingRepository;
    private readonly IConfiguration _serviceConfiguration;

    private static readonly JobKey _quartzJobKey = new(QuartzJob, QuartzGroup);

    private const string QuartzGroup = "DailyTasks";
    private const string QuartzJob = "HousekeepingAgvErrorsJob";

    /// <inheritdoc cref="Housekeeping"/>
    public Housekeeping(ISchedulerFactory schedulerFactory, ILogger<Housekeeping> logger, IServiceProvider serviceProvider, IHousekeepingConfigurationRepository configurationRepository, IConfiguration configuration)
    {
        _schedulerFactory = schedulerFactory;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _houseKeepingRepository = configurationRepository;
        _serviceConfiguration = configuration;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeConfiguration();
        var scheduler = await _schedulerFactory.GetScheduler(stoppingToken);

        var job = JobBuilder.Create<Housekeeping>().WithIdentity(_quartzJobKey).Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("HousekeepingTrigger", QuartzGroup)
            .StartNow()
            .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(1, 0))
            .ForJob(_quartzJobKey)
            .Build();

        await scheduler.ScheduleJob(job, trigger, stoppingToken);

        await scheduler.Start(stoppingToken);
    }

    private async Task InitializeConfiguration()
    {
        var dbConfig = await _houseKeepingRepository.GetConfigurationAsync();
        var config = new HousekeepingConfiguration(_serviceConfiguration);
        if (dbConfig == null)
        {
            await _houseKeepingRepository.AddOrUpdateAsync(config);
        }
    }

    /// <inheritdoc/>
    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting housekeeping of agv warnings and information");
        using var scope = _serviceProvider.CreateScope();
        var agvErrorRepository = scope.ServiceProvider.GetRequiredService<IAgvErrorRepository>();
        var houseKeepingConfigRepository = scope.ServiceProvider.GetRequiredService<IHousekeepingConfigurationRepository>();
        var config = await houseKeepingConfigRepository.GetConfigurationAsync();
        if (config == null)
        {
            _logger.LogWarning("No configuration for agv error housekeeping, not deleting any data");
            return;
        }
        await agvErrorRepository.DeleteOutdatedErrors(config.GetOutdatedTime());
    }
}