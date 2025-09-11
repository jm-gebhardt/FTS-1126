using AutoMapper;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Messages.Commands.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Handles maintenance log related commands.
/// </summary>
[UsedImplicitly]
public class MaintenanceLogCommandHandler(IAgvDb agvDb, ILogger<MaintenanceLogCommandHandler> logger, IMapper mapper) :
    IConsumer<AddMaintenanceLog>
{
    /// <summary>
    /// Handles the <see cref="AddMaintenanceLog"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AddMaintenanceLog> context)
    {
        var maintenanceLog = context.Message.MaintenanceLog;
        
        var agv = await agvDb.Agvs.GetAgvAsync(maintenanceLog.AgvCorrelationId);
        var lastMaintenance = await agvDb.MaintenanceLogs.GetLatestMaintenanceLogForAgvAsync(agv.CorrelationId);

        await agvDb.MaintenanceLogs.AddAsync(maintenanceLog);

        logger.LogAgvInformation(LogCodes.AgvMaintenanceLogCreated, agv.CorrelationId, "AGV Maintenance Log Created",
            $"Maintenance log created for AGV by {maintenanceLog.User}: {maintenanceLog.MaintenanceType}, {maintenanceLog.Comment}",
            new Dictionary<string, object?>
            {
                { LogLabels.AgvSerialNumber, agv.SerialNumber },
                { LogLabels.User, maintenanceLog.User },
                { LogLabels.MaintenanceComment, maintenanceLog.Comment },
                { LogLabels.MaintenanceType, maintenanceLog.MaintenanceType },
                { LogLabels.MaintenanceExecutionDate, maintenanceLog.ExecutionDate }
            });

        await context.Publish(new MaintenanceLogCreated(agv.CorrelationId, maintenanceLog));

        // Raise AgvMaintained event only if the current log is not for an old maintenance
        if (lastMaintenance == null || lastMaintenance.ExecutionDate <= maintenanceLog.ExecutionDate)
        {
            await context.Publish(new AgvMaintained(agv.CorrelationId, mapper.Map<Maintenance>(maintenanceLog)));
        }
    }
}