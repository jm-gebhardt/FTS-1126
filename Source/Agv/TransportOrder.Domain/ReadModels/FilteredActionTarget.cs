using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.ReadModels;

/// <inheritdoc cref="ActionTarget"/>
[PublicAPI]
public record FilteredActionTarget(FilteredAgvPosition? Position, FilteredGraphNode? Node);

/// <inheritdoc cref="AgvPosition"/>
[PublicAPI]
public record FilteredAgvPosition(float X, float Y, float Z, float Theta);

/// <inheritdoc cref="GraphNode"/>
[PublicAPI]
public record FilteredGraphNode(object Name, object Type);