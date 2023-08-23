using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;

/// <summary>
/// Provides a request to get filtered action groups.
/// </summary>
/// <param name="States">The action group states filter.</param>
/// <param name="FromDate">The earliest creation date filter. </param>
/// <param name="ToDate">The latest creation date filter.</param>
/// <param name="Limit">The limit of returned action groups.</param>
public record GetActionGroups(ActionGroupState[] States, DateTime FromDate, DateTime ToDate, int Limit = 1000);