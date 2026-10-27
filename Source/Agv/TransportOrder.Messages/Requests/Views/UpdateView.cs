using Gebhardt.Acc.TransportOrder.Domain.Models.Views;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Views;

/// <summary>Represents a command to change a view. Changing a generated view stores it.</summary>
/// <param name="CorrelationId">The id of the view.</param>
/// <param name="Name">The new name. Null keeps the current one.</param>
/// <param name="LevelId">The new level id. Null keeps the current one.</param>
/// <param name="IsDefault">Whether the view is opened first. Null keeps the current value.</param>
/// <param name="Area">The new area. Null keeps the current one.</param>
public record UpdateView(Guid CorrelationId, string? Name, string? LevelId, bool? IsDefault, ViewArea? Area) : CorrelatedBy<Guid>;