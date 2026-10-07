using Gebhardt.Acc.DeviceCommunication.Common.Messages.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.EventHandlers;

/// <summary>
/// Handles factsheet events.
/// </summary>
[UsedImplicitly]
public class FactsheetEventHandler : IConsumer<FactsheetUploaded>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<FactsheetUploaded> context)
    {
        (var agvTypeId, string agvTypeIdentifier, string factsheetJson) = context.Message;
        await context.Publish(new CacheFactsheet(agvTypeId, agvTypeIdentifier, factsheetJson));
    }
}