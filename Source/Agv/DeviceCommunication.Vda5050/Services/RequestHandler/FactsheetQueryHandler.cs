using Gebhardt.Acc.DeviceCommunication.Common.Messages.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.RequestHandler;

/// <summary>
/// Handles factsheet queries.
/// </summary>
[UsedImplicitly]
public class FactsheetQueryHandler(IAgvTypeRepository agvTypeRepository) : IConsumer<GetFactsheet>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<GetFactsheet> context)
    {
        (var correlationId, string agvTypeIdentifier) = context.Message;
        var agvType = await agvTypeRepository.GetAgvTypeAsync(agvTypeIdentifier);
        if (string.IsNullOrWhiteSpace(agvType?.RawData))
        {
            await context.RespondAsync(new FactsheetNotFound(correlationId, agvTypeIdentifier));
            return;
        }

        await context.RespondAsync(new GotFactsheet(correlationId, agvTypeIdentifier, agvType.RawData));
    }
}