using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.ReadModels;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Actions.Common;

/// <summary>
/// Represents the response to the command <see cref="get_AgvGroupCountByState"/>.
/// </summary>
/// <param name="AgvGroupCountByState">The agv group count by state.</param>
public record GotActionGroupCountsByState(IList<AgvGroupCountByState> AgvGroupCountByState);