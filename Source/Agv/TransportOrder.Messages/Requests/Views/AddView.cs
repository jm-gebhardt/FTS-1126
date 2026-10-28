using Gebhardt.Acc.TransportOrder.Domain.Models.Views;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Views;

/// <summary>Represents a command to store a new view.</summary>
/// <param name="CorrelationId">The id of the new view.</param>
/// <param name="Name">The name of the view.</param>
/// <param name="LevelId">The id of the level the view shows.</param>
/// <param name="IsDefault">Whether the view is opened first.</param>
/// <param name="Area">The area of the graph the view shows.</param>
public record AddView(Guid CorrelationId, string Name, string? LevelId, bool IsDefault, ViewArea Area) : CorrelatedBy<Guid>;