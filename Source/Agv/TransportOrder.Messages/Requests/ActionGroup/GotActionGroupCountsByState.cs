using TransportOrder.Domain.ReadModels;

namespace TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Represents the response to the command <see cref="get_AgvGroupCountByState"/>.
/// </summary>
/// <param name="AgvGroupCountByState">The agv group count by state.</param>
public record GotActionGroupCountsByState(IList<AgvGroupCountByState> AgvGroupCountByState);