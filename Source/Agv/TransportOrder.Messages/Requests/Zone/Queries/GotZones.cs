namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Zone.Queries;

/// <summary>
/// Defines a result for the <see cref="GetZones"/> request.
/// </summary>
public record GotZones(ICollection<Domain.Models.Zone.Zone> Zones);