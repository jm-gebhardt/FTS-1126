using Gebhardt.Acc.DeviceCommunication.Common.Messages.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Services.CommandHandlers;

/// <summary>
/// Handles factsheet commands.
/// </summary>
[UsedImplicitly]
public class FactsheetCommandHandler(
    ILogger<FactsheetCommandHandler> logger,
    IFactsheetCache factsheetCache,
    IRequestClient<GetFactsheet> requestClient) :
    IConsumer<CacheFactsheet>,
    IConsumer<FetchFactsheet>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<CacheFactsheet> context)
    {
        (var agvTypeId, string agvTypeIdentifier, string factsheetJson) = context.Message;
        await factsheetCache.AddOrUpdateAsync(agvTypeId, factsheetJson);
        logger.LogInformation("Cached factsheet of agv type {AgvTypeIdentifier}", agvTypeIdentifier);
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<FetchFactsheet> context)
    {
        (var agvTypeId, string agvTypeIdentifier) = context.Message;
        try
        {
            Response<GotFactsheet, FactsheetNotFound> response = await requestClient.GetResponse<GotFactsheet, FactsheetNotFound>(
                new GetFactsheet(agvTypeId, agvTypeIdentifier), context.CancellationToken);
            if (response.Is(out Response<GotFactsheet>? gotFactsheet))
            {
                await factsheetCache.AddOrUpdateAsync(agvTypeId, gotFactsheet.Message.FactsheetJson);
                logger.LogInformation("Fetched factsheet of agv type {AgvTypeIdentifier}", agvTypeIdentifier);
                return;
            }

            logger.LogInformation("No factsheet uploaded for agv type {AgvTypeIdentifier}", agvTypeIdentifier);
        }
        catch (RequestException ex)
        {
            logger.LogWarning(ex, "Failed to fetch factsheet of agv type {AgvTypeIdentifier}", agvTypeIdentifier);
        }
    }
}