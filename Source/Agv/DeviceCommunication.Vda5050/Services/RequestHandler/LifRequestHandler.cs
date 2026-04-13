using FluentValidation;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Graph;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Lif;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using Vdma.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Services.RequestHandler;

/// <summary>
/// Handles <see cref="AddOrUpdateLif"/> requests.
/// Validates the LIF, converts it to graph layout domain models,
/// and sends a <see cref="StoreGraphLayouts"/> request to the shared storage handler.
/// </summary>
[UsedImplicitly]
public class LifRequestHandler(
    IValidator<LifLayoutCollection> lifValidator,
    IRequestClient<StoreGraphLayouts> storeGraphLayoutsClient,
    TimeProvider timeProvider,
    ILogger<LifRequestHandler> logger) : IConsumer<AddOrUpdateLif>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AddOrUpdateLif> context)
    {
        var lifLayoutCollection = context.Message.LifLayoutCollection;

        var validationResult = await lifValidator.ValidateAsync(lifLayoutCollection);
        if (!validationResult.IsValid)
        {
            string errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            logger.LogError("LIF validation failed: {Errors}", errors);
            throw new ValidationException(validationResult.Errors);
        }

        ICollection<GraphLayout> layouts = lifLayoutCollection.ToGraphLayouts(timeProvider.GetUtcNow().UtcDateTime, lifLayoutCollection.MetaInformation.ExportTimestamp.DateTime);

        logger.LogInformation("LIF converted to {LayoutCount} graph layouts, sending StoreGraphLayouts request", layouts.Count);
        await storeGraphLayoutsClient.GetResponse<GraphLayoutStored>(new StoreGraphLayouts(layouts),
            context.CancellationToken
        );

        await context.RespondAsync(new Success());
    }
}