using System.Globalization;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Filter.Api;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.ActionGroup;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterResults;
using JetBrains.Annotations;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gebhardt.Acc.TransportOrder.WebApi.Swagger.Examples;

/// <summary>
/// Adds an example body to the <see cref="ActionGroupsController.Filtered"/> endpoint.
/// </summary>
[UsedImplicitly]
public class ActionGroupFilterExample : IOperationFilter
{
    /// <summary>
    /// The example dto that will be attached by this operation filter.
    /// </summary>
    [PublicAPI]
    public static readonly FilterDto ExampleDto = new CombinationFilterDto(
        [
            new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.CreatedDate), ComparisonVariant.Gt, DateTime.UtcNow.AddHours(-1).ToString(CultureInfo.CurrentCulture)),
            new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.CreatedDate), ComparisonVariant.Lt, DateTime.UtcNow.AddHours(1).ToString(CultureInfo.CurrentCulture)),
            new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.StartedDate), ComparisonVariant.Gt, DateTime.UtcNow.AddHours(-1).ToString(CultureInfo.CurrentCulture)),
            new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.StartedDate), ComparisonVariant.Lt, DateTime.UtcNow.AddHours(1).ToString(CultureInfo.CurrentCulture)),
            new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.CompletedDate), ComparisonVariant.Gt, DateTime.UtcNow.AddHours(-1).ToString(CultureInfo.CurrentCulture)),
            new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.CompletedDate), ComparisonVariant.Lt, DateTime.UtcNow.AddHours(1).ToString(CultureInfo.CurrentCulture)),
            new CombinationFilterDto([
                new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.AssignedAgv) + IFilter.AccessLevelSeparator + nameof(FilteredAgvDto.CorrelationId), ComparisonVariant.Equals, Guid.NewGuid().ToString()),
                new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.AssignedAgv) + IFilter.AccessLevelSeparator + nameof(FilteredAgvDto.CorrelationId), ComparisonVariant.Equals, Guid.NewGuid().ToString()),
                new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.AssignedAgv) + IFilter.AccessLevelSeparator + nameof(FilteredAgvDto.CorrelationId), ComparisonVariant.Equals, Guid.NewGuid().ToString())
            ], LogicalCombinationVariant.Or),
            new CombinationFilterDto([
                new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.HandlingUnit), ComparisonVariant.Like, "HU012"),
                new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.HandlingUnit), ComparisonVariant.Like, "HU0112")
            ], LogicalCombinationVariant.Or),
            new CollectionFilterDto(
                new CombinationFilterDto([
                    new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.Actions) + IFilter.AccessLevelSeparator + nameof(FilteredActionDto.ActionType), ComparisonVariant.Equals, nameof(ActionType.Charge)),
                    new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.Actions) + IFilter.AccessLevelSeparator + nameof(FilteredActionDto.ActionType), ComparisonVariant.Equals, nameof(ActionType.Pick))
                ], LogicalCombinationVariant.Or),
                nameof(FilteredActionGroupDto.Actions),
                CollectionAggregateVariant.Any
            ),
            new CollectionFilterDto(
                new CombinationFilterDto([
                    new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.Actions) + IFilter.AccessLevelSeparator + nameof(FilteredActionDto.ActionType), ComparisonVariant.Equals, nameof(ActionType.Pick)),
                    new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.Actions) + IFilter.AccessLevelSeparator + nameof(FilteredActionDto.Target) + IFilter.AccessLevelSeparator + nameof(FilteredActionTargetDto.Node) + IFilter.AccessLevelSeparator + nameof(FilteredGraphNodeDto.Name), ComparisonVariant.Equals, "ExampleSourceNodeName")
                ], LogicalCombinationVariant.And),
                nameof(FilteredActionGroupDto.Actions),
                CollectionAggregateVariant.Any
            ),
            new CollectionFilterDto(
                new CombinationFilterDto( [
                    new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.Actions) + IFilter.AccessLevelSeparator + nameof(FilteredActionDto.ActionType), ComparisonVariant.Equals, nameof(ActionType.Drop)),
                    new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.Actions) + IFilter.AccessLevelSeparator + nameof(FilteredActionDto.Target) + IFilter.AccessLevelSeparator + nameof(FilteredActionTargetDto.Node) + IFilter.AccessLevelSeparator + nameof(FilteredGraphNodeDto.Name), ComparisonVariant.Equals, "ExampleTargetNodeName")
                ], LogicalCombinationVariant.And),
                nameof(FilteredActionGroupDto.Actions),
                CollectionAggregateVariant.Any
            ),
            new CombinationFilterDto([
                new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.EstimatedDuration), ComparisonVariant.Gt, "0"),
                new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.EstimatedDuration), ComparisonVariant.Lt, TimeSpan.MaxValue.ToString())
            ], LogicalCombinationVariant.Or),
            new CombinationFilterDto([
                new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.State), ComparisonVariant.Equals, nameof(ActionGroupState.Completed)),
                new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.State), ComparisonVariant.Equals, nameof(ActionGroupState.Canceled)),
                new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.State), ComparisonVariant.Equals, nameof(ActionGroupState.Aborted))
            ], LogicalCombinationVariant.Or),
            new CollectionFilterDto(
                new CombinationFilterDto([
                    new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.LogEntries) + IFilter.AccessLevelSeparator + nameof(FilteredLogEntryDto.LogLevel), ComparisonVariant.Equals, "Error"),
                    new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.LogEntries) + IFilter.AccessLevelSeparator + nameof(FilteredLogEntryDto.LogLevel), ComparisonVariant.Equals, "Warning"),
                    new ValueComparisonFilterDto(nameof(FilteredActionGroupDto.LogEntries) + IFilter.AccessLevelSeparator + nameof(FilteredLogEntryDto.LogCode), ComparisonVariant.Equals, "117")
                ], LogicalCombinationVariant.Or),
                nameof(FilteredActionGroupDto.LogEntries),
           CollectionAggregateVariant.Where
            ),
            new CollectionFilterDto(
                new ValueComparisonFilterDto(
                    nameof(FilteredActionGroupDto.Actions) + IFilter.AccessLevelSeparator + nameof(FilteredActionDto.Target) + IFilter.AccessLevelSeparator + nameof(FilteredActionTargetDto.Node) + IFilter.AccessLevelSeparator + nameof(FilteredGraphNodeDto.Type),
                    ComparisonVariant.Like,
                    nameof(AgvGraph.NodeType.Pick)),
                nameof(FilteredActionGroupDto.Actions),
                CollectionAggregateVariant.Any
            ),
            new CollectionFilterDto(
                new ValueComparisonFilterDto(
                    nameof(FilteredActionGroupDto.Actions) + IFilter.AccessLevelSeparator + nameof(FilteredActionDto.Target) + IFilter.AccessLevelSeparator + nameof(FilteredActionTargetDto.Node) + IFilter.AccessLevelSeparator + nameof(FilteredGraphNodeDto.Type),
                    ComparisonVariant.Like,
                    nameof(AgvGraph.NodeType.Drop)),
                nameof(FilteredActionGroupDto.Actions),
                CollectionAggregateVariant.Any
            )
        ],
        LogicalCombinationVariant.And
    );

    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.Name != nameof(ActionGroupsController.Filtered))
        {
            return;
        }
        
        if (operation.RequestBody?.Content != null)
        {
            operation.RequestBody.Content["application/json"].Example = ExampleDto.ToOpenApiObject();
        }
    }
}