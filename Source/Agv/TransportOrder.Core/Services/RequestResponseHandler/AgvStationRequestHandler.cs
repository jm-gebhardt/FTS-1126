using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using RemoveAgvFromStation = Gebhardt.Acc.Common.Messages.Stations.Commands.RemoveAgvFromStation;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for agv station related requests.
/// </summary>
[UsedImplicitly]
public class AgvStationRequestHandler :
    IConsumer<Messages.Requests.Stations.RemoveAgvFromStation>
{
    private readonly ILogger<AgvStationRequestHandler> _logger;
    private readonly IAgvDb _agvDb;
    private readonly IBus _bus;
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Defines a handler for agv station related requests.
    /// </summary>
    public AgvStationRequestHandler(ILogger<AgvStationRequestHandler> logger,
        IAgvDb agvDb,
        IBus bus, ILoggingService loggingService)
    {
        _logger = logger;
        _agvDb = agvDb;
        _bus = bus;
        _loggingService = loggingService;
    }

    /// <summary>
    /// Handles the <see cref="Messages.Requests.Stations.RemoveAgvFromStation"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<Messages.Requests.Stations.RemoveAgvFromStation> context)
    {
        (string stationName, var agvId, string user) = context.Message;

        var agvToRemove = await _agvDb.Agvs.TryGetAgvAsync(agvId);

        if (agvToRemove == null)
        {
            throw new AgvNotFoundException(agvId);
        }

        var onlineAgv = await _agvDb.Agvs.GetAnyOnlineAgv();

        if (onlineAgv == null)
        {
            throw new AllAgvsOfflineException();
        }

        await _bus.Publish(new RemoveAgvFromStation(stationName, agvToRemove.SerialNumber, onlineAgv.SerialNumber));
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, agvId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvRemoveFromNode } });

        await context.RespondAsync(new Success());
    }
}