using Gebhardt.Acc.TransportOrder.Domain.ReadModels;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Represents the response to <see cref="ActionGroup"/> when an action group has been created.
/// </summary>
/// <param name="ActionGroup"></param>
public record ActionGroupCreated(ActionGroupReadModel ActionGroup);