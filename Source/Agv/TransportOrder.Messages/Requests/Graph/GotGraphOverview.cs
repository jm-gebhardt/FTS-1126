using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using NewGraph = Gebhardt.Acc.Common.Domain.Models.NewGraph.Graph;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;

/// <summary>
/// Response with the global graph together with the load states of all AGV types.
/// </summary>
/// <param name="Graph">The global graph.</param>
/// <param name="LoadStates">The load states of all AGV types.</param>
public record GotGraphOverview(NewGraph Graph, IReadOnlyList<LoadState> LoadStates);