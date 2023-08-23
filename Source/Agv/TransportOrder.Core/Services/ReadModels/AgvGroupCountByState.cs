using Gebhardt.StoreWare.Agv.Common.Messages.Actions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.ReadModels;

/// <summary>
///Provides a read model for action group counts by state.
/// </summary>
/// <param name="State">The action group state.</param>
/// <param name="Count">Number of actions groups in a state.</param>
public record AgvGroupCountByState(ActionGroupState State, int Count);