using Gebhardt.StoreWare.Agv.TransportOrder.Domain.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Represents the response to the command <see cref="GetActionGroup"/>.
/// </summary>
/// <param name="AgvActionGroup">The action group returned in the response.</param>
public record GotAgvActionGroup(ActionGroupReadModel AgvActionGroup);