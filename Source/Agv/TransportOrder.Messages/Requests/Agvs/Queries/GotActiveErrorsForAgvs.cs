using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;

/// <summary>
/// Defines a result for the <see cref="GetActiveErrorsForAgvs"/> request.
/// </summary>
public record GotActiveErrorsForAgvs(Dictionary<Guid, List<AgvError>> ErrorsByAgv);