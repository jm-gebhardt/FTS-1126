using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles emission of requests for new agv information.
/// </summary>
[UsedImplicitly]
public class EmitAgvRequestsHandler(ILogger<EmitAgvRequestsHandler> logger, IAgvDb agvDb, IBus bus) : 
    IConsumer<RequestAgvGraph>,
    IConsumer<RequestAgvData>,
    IConsumer<RequestAgvZones>
{
    /// <summary>
    /// Handles the <see cref="RequestAgvGraph"/> command.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<RequestAgvGraph> context)
    {
        var correlationId = context.Message.CorrelationId;
        string user = context.Message.User;
        var agv = await agvDb.Agvs.GetAgvAsync(correlationId);
        await bus.Publish(new Common.Messages.AgvManagement.Commands.RequestAgvGraph(agv.CorrelationId, agv.SerialNumber));
        logger.LogAgvInformation(LogCodes.AgvOperatorInteraction, correlationId, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvGraphRequested} });

        await context.RespondAsync(new RequestAgvGraphResult(agv.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="RequestAgvData"/> request.
    /// </summary>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<RequestAgvData> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        var agv = await agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        await bus.Publish(new Common.Messages.AgvManagement.Commands.RequestAgvData(agv.CorrelationId, agv.SerialNumber));
        logger.LogAgvInformation(LogCodes.AgvOperatorInteraction, agvCorrelationId, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, context.Message.User }, { LogLabels.Operation, LogCodes.AgvDataRequested} });

        await context.RespondAsync(new RequestedAgvData(agv.CorrelationId));
    }

    /// <summary>
    /// Handles the <see cref="RequestAgvZones"/> request.
    /// </summary>
    /// <exception cref="AgvNotFoundException"></exception>
    public async Task Consume(ConsumeContext<RequestAgvZones> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        var agv = await agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        await bus.Publish(new Common.Messages.AgvManagement.Commands.RequestAgvZones(agv.CorrelationId, agv.SerialNumber));
        logger.LogAgvInformation(LogCodes.AgvOperatorInteraction, agvCorrelationId, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, context.Message.User }, { LogLabels.Operation, LogCodes.AgvZonesRequested } });

        await context.RespondAsync(new Success());
    }
}