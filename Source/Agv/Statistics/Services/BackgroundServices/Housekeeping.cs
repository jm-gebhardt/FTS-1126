using Gebhardt.Acc.Statistics.Domain.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Gebhardt.Acc.Statistics.Services.BackgroundServices;

/// <summary>
/// Background service for statistics housekeeping.
/// </summary>
public class Housekeeping : BackgroundService, IJob
{
    private readonly IHouseKeepingRepository _houseKeepingRepository;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<Housekeeping> _logger;

    private static readonly JobKey _quartzJobKey = new(QuartzJob, QuartzGroup);

    private const string QuartzGroup = "DailyTasks";
    private const string QuartzJob = "HousekeepingJob";

    /// <inheritdoc cref="Housekeeping"/>
    public Housekeeping(IHouseKeepingRepository houseKeepingRepository, ISchedulerFactory schedulerFactory, ILogger<Housekeeping> logger)
    {
        _houseKeepingRepository = houseKeepingRepository;
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    ///<inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        var job = JobBuilder.Create<Housekeeping>().WithIdentity(_quartzJobKey).Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("HousekeepingTrigger", QuartzGroup)
            .StartNow()
            .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(0, 0))
            .ForJob(_quartzJobKey)
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);

        await scheduler.Start(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task Execute(IJobExecutionContext _)
    {
        _logger.LogInformation("Starting compression of statistics"); 
        await _houseKeepingRepository.CompressStatistics();
    }
}
