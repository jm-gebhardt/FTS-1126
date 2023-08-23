using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;

/// <summary>
/// Represents the response to the command <see cref="GetActionGroup"/>.
/// </summary>
/// <param name="AgvActionGroup">The action group returned in the response.</param>
public record GotAgvActionGroup(ActionGroupReadModel AgvActionGroup);