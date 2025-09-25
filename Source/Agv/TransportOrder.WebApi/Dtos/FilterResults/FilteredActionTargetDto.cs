using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Filter.Api.Swagger;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterResults;

/// <inheritdoc cref="ActionTarget"/>
[PublicAPI]
public record FilteredActionTargetDto(FilteredAgvPositionDto? Position, FilteredGraphNodeDto? Node);

/// <inheritdoc cref="AgvPosition"/>
[PublicAPI]
public record FilteredAgvPositionDto([property: SwaggerFilterableProperty] float X, [property: SwaggerFilterableProperty] float Y, [property: SwaggerFilterableProperty] float Z, [property: SwaggerFilterableProperty] float Theta);

/// <inheritdoc cref="GraphNode"/>
[PublicAPI]
public record FilteredGraphNodeDto([property: SwaggerFilterableProperty]string Name);