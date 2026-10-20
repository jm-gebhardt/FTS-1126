using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Common.Messages.Factsheets;
using Gebhardt.Acc.Vda5050Simulator.Domain;
using Gebhardt.Acc.Vda5050Simulator.Runtime;
using MassTransit;
using System.Collections.Concurrent;

namespace Gebhardt.Acc.Vda5050Simulator.Messaging;

/// <summary>
/// Fetches factsheets from DeviceCommunication.Vda5050 via MassTransit request/response and caches them per agv type.
/// </summary>
public sealed class MassTransitFactsheetProvider(IBus bus, ILogger<MassTransitFactsheetProvider> logger) : IFactsheetProvider
{
    private readonly ConcurrentDictionary<string, string> _cache = new();

    public async Task<string?> GetAsync(AgvIdentity agvIdentity, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(agvIdentity.AgvTypeIdentifier, out var cached))
        {
            return cached;
        }

        string? factsheet = await TryGetFactsheet(agvIdentity, cancellationToken);
        if (factsheet != null)
        {
            return factsheet;
        }
        
        logger.LogInformation("No factsheet uploaded for agv type {AgvTypeIdentifier}", agvIdentity);
        return null;
    }

    private async Task<string?> TryGetFactsheet(AgvIdentity agvIdentity, CancellationToken cancellationToken)
    {
        var client = bus.CreateRequestClient<GetFactsheet>();
        var response = await client.GetResponse<GotFactsheet, FactsheetNotFound>(
            new GetFactsheet(agvIdentity.SerialNumber.ToGuid(), agvIdentity.AgvTypeIdentifier), cancellationToken);
        if (!response.Is(out Response<GotFactsheet>? gotFactsheet))
        {
            return null;
        }

        _cache[agvIdentity.AgvTypeIdentifier] = gotFactsheet.Message.FactsheetJson;

        return gotFactsheet.Message.FactsheetJson;
    }
}
