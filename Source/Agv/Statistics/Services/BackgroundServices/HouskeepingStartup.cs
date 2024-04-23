using Gebhardt.Acc.Statistics.Messages.Commands;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Gebhardt.Acc.Statistics.Services.BackgroundServices;

/// <summary>
/// Background service for agv position housekeeping.
/// </summary>
public class HousekeepingStartup : BackgroundService, IJob
{
    private readonly IBus _bus;
    private readonly ISchedulerFactory _schedulerFactory;

    private static readonly JobKey _quartzJobKey = new(QuartzJob, QuartzGroup);

    private const string QuartzGroup = "DailyTasks";
    private const string QuartzJob = "AgvPositionHousekeepingJob";

    /// <inheritdoc cref="HousekeepingStartup"/>
    public HousekeepingStartup(IBus bus, ISchedulerFactory schedulerFactory)
    {
        _bus = bus;
        _schedulerFactory = schedulerFactory;
    }

    ///<inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        var job = JobBuilder.Create<HousekeepingStartup>().WithIdentity(_quartzJobKey).Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("AgvPositionHousekeepingTrigger", QuartzGroup)
            .StartNow()
            .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(0,0))
            .ForJob(_quartzJobKey)
            .Build();

        await scheduler.ScheduleJob(job, trigger, cancellationToken);

        await scheduler.Start(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task Execute(IJobExecutionContext _)
    {
        await _bus.Publish(new HousekeepAgvPositions());
    }
}