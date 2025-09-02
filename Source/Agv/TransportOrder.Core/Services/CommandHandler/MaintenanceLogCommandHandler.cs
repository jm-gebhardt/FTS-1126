using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.MaintenanceLog;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Handles maintenance log related commands.
/// </summary>
[UsedImplicitly]
public class MaintenanceLogCommandHandler(IMaintenanceLogRepository maintenanceLogRepository, IAgvDb agvDb, ILogger<MaintenanceLogCommandHandler> logger) :
    IConsumer<AddMaintenanceLog>
{
    /// <summary>
    /// Handles the <see cref="AddMaintenanceLog"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<AddMaintenanceLog> context)
    {
        var maintenanceLog = context.Message.MaintenanceLog;
        await maintenanceLogRepository.AddAsync(maintenanceLog);
        var agv = await agvDb.Agvs.GetAgvAsync(maintenanceLog.AgvCorrelationId);

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
    }
}