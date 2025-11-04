using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Filter.Api.Swagger;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterResults;

/// <inheritdoc cref="ActionTarget"/>
[PublicAPI]
public record FilteredActionTargetDto(FilteredAgvPositionDto? Position, FilteredGraphNodeDto? Node)
{
    /// <inheritdoc cref="ActionTarget.Position"/>
    public FilteredAgvPositionDto? Position { get; init; } = Position;

    /// <inheritdoc cref="ActionTarget.Node"/>
    public FilteredGraphNodeDto? Node { get; init; } = Node;
}

/// <inheritdoc cref="AgvPosition"/>
[PublicAPI]
public record FilteredAgvPositionDto(float X, float Y, float Z, float Theta)
{
    /// <inheritdoc cref="AgvPosition.X"/>
    [SwaggerFilterableProperty]
    public float X { get; init; } = X;

    /// <inheritdoc cref="AgvPosition.Y"/>
    [SwaggerFilterableProperty]
    public float Y { get; init; } = Y;

    /// <inheritdoc cref="AgvPosition.Z"/>
    [SwaggerFilterableProperty]
    public float Z { get; init; } = Z;

    /// <inheritdoc cref="AgvPosition.Theta"/>
    [SwaggerFilterableProperty]
    public float Theta { get; init; } = Theta;
}

/// <inheritdoc cref="GraphNode"/>
[PublicAPI]
public record FilteredGraphNodeDto(string Name)
{
    /// <inheritdoc cref="BaseNode.Name"/>
    [SwaggerFilterableProperty]
    public string Name { get; init; } = Name;
}