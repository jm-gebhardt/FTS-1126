using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Zone.Queries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles zone related requests.
/// </summary>
[UsedImplicitly]
public class ZoneRequestHandler(IZoneRepository zoneRepository): IConsumer<GetZones>
{
    /// <summary>
    /// Handles <see cref="GetZones"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<GetZones> context)
    {
        var cancellationToken = context.CancellationToken;
        ICollection<Zone> zones = await zoneRepository.GetZones(cancellationToken);
        await context.RespondAsync(new GotZones(zones));
    }
}