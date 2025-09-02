using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.MaintenanceLog;
using Gebhardt.Acc.TransportOrder.Messages.Requests.MaintenanceLog;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles maintenance log related requests.
/// </summary>
[UsedImplicitly]
public class MaintenanceLogRequestHandler(IBus bus, ILogger<MaintenanceLogRequestHandler> logger, IAgvDb agvDb, IMaintenanceLogRepository maintenanceLogRepository) :
    IConsumer<TriggerAddingMaintenanceLog>,
    IConsumer<GetMaintenanceLogsForAgv>,
    IConsumer<GetMaintenanceLogsForAgvs>
{
    /// <summary>
    /// Handles the <see cref="TriggerAddingMaintenanceLog"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<TriggerAddingMaintenanceLog> context)
    {
        (string user, var agv, var type, var date, string? comment) = context.Message;

        await agvDb.Agvs.ThrowIfAgvNotExistsAsync(agv);

        logger.LogInformation("{User} triggered adding maintenance log entry for agv {AgvCorrelationId}: {MaintenanceType}/{Comment}/{ExecutionDate}", user, agv, type, comment, date);

        await bus.Publish(new AddMaintenanceLog(new MaintenanceLog(user, agv, date, type, comment)));

        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="GetMaintenanceLogsForAgv"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetMaintenanceLogsForAgv> context)
    {
        (var agv, MaintenanceType? type) = context.Message;

        await agvDb.Agvs.ThrowIfAgvNotExistsAsync(agv);

        IReadOnlyCollection<MaintenanceLog> logs = await maintenanceLogRepository.GetMaintenancesLogsAsync([agv], type);

        await context.RespondAsync(new GotMaintenanceLogs(logs.ToList()));
    }

    /// <summary>
    /// Handles the <see cref="GetMaintenanceLogsForAgvs"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetMaintenanceLogsForAgvs> context)
    {
        (ICollection<Guid> agvs, MaintenanceType? type) = context.Message;

        await agvDb.Agvs.ThrowIfAgvsNotExistAsync(agvs);

        IReadOnlyCollection<MaintenanceLog> logs = await maintenanceLogRepository.GetMaintenancesLogsAsync(agvs, type);

        await context.RespondAsync(new GotMaintenanceLogs(logs.ToList()));
    }
}