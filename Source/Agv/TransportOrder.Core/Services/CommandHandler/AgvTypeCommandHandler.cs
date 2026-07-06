using AutoMapper;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.AgvType;
using Gebhardt.Acc.TransportOrder.Messages.Events.AgvType;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Provides a handler for AGV type related commands.
/// </summary>
[UsedImplicitly]
public class AgvTypeCommandHandler(IAgvDb agvDb, IMapper mapper, ILoggingService loggingService, ILogger<AgvTypeCommandHandler> logger) : 
    IConsumer<AddOrUpdateAgvType>,
    IConsumer<AcknowledgeAgvType>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AddOrUpdateAgvType> context)
    {
        var info = context.Message.AgvType;
        bool alreadyExists = await agvDb.AgvTypes.ExistsAsync(info.AgvTypeIdentifier);

        var agvType = mapper.Map<AgvType>(info);
        await agvDb.AgvTypes.AddOrUpdateAsync(agvType, context.CancellationToken);

        if (alreadyExists)
        {
            logger.LogInformation("Updated AGV type {AgvTypeIdentifier}", info.AgvTypeIdentifier);
            return;
        }

        logger.LogInformation("Created AGV type {AgvTypeIdentifier}", info.AgvTypeIdentifier);
        await loggingService.LogSystem(logger, LogCodes.SystemAgvTypeCreated, "AGV type created", $"AGV type {info.AgvTypeIdentifier} was created", new Dictionary<string, object?> { { LogLabels.AgvTypeIdentifier, agvType.AgvTypeIdentifier } });
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AcknowledgeAgvType> context)
    {
        var agvType = await agvDb.AgvTypes.GetAgvTypeAsync(context.Message.CorrelationId, context.CancellationToken);

        agvType.Acknowledge();
        await agvDb.AgvTypes.AddOrUpdateAsync(agvType, context.CancellationToken);
        
        logger.LogInformation("Acknowledged AGV type {AgvTypeIdentifier}", agvType.AgvTypeIdentifier);
        await loggingService.LogSystem(logger, LogCodes.SystemAgvTypeAcknowledged, "AGV type acknowledged", $"AGV type {agvType.AgvTypeIdentifier} was acknowledged", new Dictionary<string, object?>{{LogLabels.AgvTypeIdentifier, agvType.AgvTypeIdentifier}});
        
        await context.Publish(new AgvTypeAcknowledged(agvType.Id));
    }
}