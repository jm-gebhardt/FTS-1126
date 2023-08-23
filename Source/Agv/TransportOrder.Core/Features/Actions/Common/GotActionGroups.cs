using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;

/// <summary>
/// Represents the response to the command <see cref="GetActionGroups"/>.
/// </summary>
/// <param name="ActionGroups">The action groups returned in the response.</param>
public record GotActionGroups(IReadOnlyList<ActionGroupReadModel> ActionGroups);