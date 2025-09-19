using System.Globalization;
using Gebhardt.Acc.Common.Filter.Dtos;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.ActionGroup;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.FilterResults;
using JetBrains.Annotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Gebhardt.Acc.TransportOrder.WebApi.Swagger.Examples;

/// <summary>
/// 
/// </summary>
[UsedImplicitly]
public class ActionGroupFilterExample : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.Name != nameof(ActionGroupsController.Filtered))
        {
            return;
        }

        FilterBaseDto example = new CombinationFilterDto(
            [
                new CombinationFilterDto([], [
                    new FilterLeafDto(nameof(FilteredActionGroupDto.AssignedAgv) + FilterBase.AccessLevelSeparator + nameof(FilteredAgvDto.CorrelationId), ComparisonVariant.Equals, Guid.NewGuid().ToString()),
                    new FilterLeafDto(nameof(FilteredActionGroupDto.AssignedAgv) + FilterBase.AccessLevelSeparator + nameof(FilteredAgvDto.CorrelationId), ComparisonVariant.Equals, Guid.NewGuid().ToString()),
                    new FilterLeafDto(nameof(FilteredActionGroupDto.AssignedAgv) + FilterBase.AccessLevelSeparator + nameof(FilteredAgvDto.CorrelationId), ComparisonVariant.Equals, Guid.NewGuid().ToString())
                ], LogicalCombinationVariant.Or),
                new CombinationFilterDto([], [
                    new FilterLeafDto(nameof(FilteredActionGroupDto.HandlingUnit), ComparisonVariant.Like, "HU012"),
                    new FilterLeafDto(nameof(FilteredActionGroupDto.HandlingUnit), ComparisonVariant.Like, "HU0112")
                ], LogicalCombinationVariant.Or),
                new CollectionFilterDto(
                    new CombinationFilterDto([], [
                        new FilterLeafDto(nameof(FilteredActionGroupDto.Actions) + FilterBase.AccessLevelSeparator + nameof(FilteredActionDto.ActionType), ComparisonVariant.Equals, nameof(ActionType.Charge)),
                        new FilterLeafDto(nameof(FilteredActionGroupDto.Actions) + FilterBase.AccessLevelSeparator + nameof(FilteredActionDto.ActionType), ComparisonVariant.Equals, nameof(ActionType.Pick))
                    ], LogicalCombinationVariant.Or),
                    nameof(FilteredActionGroupDto.Actions),
                    CollectionAggregateVariant.Any
                ),
                new CollectionFilterDto(
                    new CombinationFilterDto([], [
                        new FilterLeafDto(nameof(FilteredActionGroupDto.Actions) + FilterBase.AccessLevelSeparator + nameof(FilteredActionDto.ActionType), ComparisonVariant.Equals, nameof(ActionType.Pick)),
                        new FilterLeafDto(nameof(FilteredActionGroupDto.Actions) + FilterBase.AccessLevelSeparator + nameof(FilteredActionDto.Target) + FilterBase.AccessLevelSeparator + nameof(FilteredActionTargetDto.Node) + FilterBase.AccessLevelSeparator + nameof(FilteredGraphNodeDto.Name), ComparisonVariant.Equals, "ExampleSourceNodeName")
                    ], LogicalCombinationVariant.And),
                    nameof(FilteredActionGroupDto.Actions),
                    CollectionAggregateVariant.Any
                ),
                new CollectionFilterDto(
                    new CombinationFilterDto([], [
                        new FilterLeafDto(nameof(FilteredActionGroupDto.Actions) + FilterBase.AccessLevelSeparator + nameof(FilteredActionDto.ActionType), ComparisonVariant.Equals, nameof(ActionType.Drop)),
                        new FilterLeafDto(nameof(FilteredActionGroupDto.Actions) + FilterBase.AccessLevelSeparator + nameof(FilteredActionDto.Target) + FilterBase.AccessLevelSeparator + nameof(FilteredActionTargetDto.Node) + FilterBase.AccessLevelSeparator + nameof(FilteredGraphNodeDto.Name), ComparisonVariant.Equals, "ExampleTargetNodeName")
                    ], LogicalCombinationVariant.And),
                    nameof(FilteredActionGroupDto.Actions),
                    CollectionAggregateVariant.Any
                ),
                new CombinationFilterDto([], [
                    new FilterLeafDto(nameof(FilteredActionGroupDto.EstimatedDuration), ComparisonVariant.Gt, TimeSpan.MinValue.ToString()),
                    new FilterLeafDto(nameof(FilteredActionGroupDto.EstimatedDuration), ComparisonVariant.Lt, TimeSpan.MaxValue.ToString())
                ], LogicalCombinationVariant.Or),
                new CombinationFilterDto([], [
                    new FilterLeafDto(nameof(FilteredActionGroupDto.State), ComparisonVariant.Equals, nameof(ActionGroupState.Completed)),
                    new FilterLeafDto(nameof(FilteredActionGroupDto.State), ComparisonVariant.Equals, nameof(ActionGroupState.Canceled)),
                    new FilterLeafDto(nameof(FilteredActionGroupDto.State), ComparisonVariant.Equals, nameof(ActionGroupState.Aborted))
                ], LogicalCombinationVariant.Or)
            ],
            [
                new FilterLeafDto(nameof(FilteredActionGroupDto.CreatedDate), ComparisonVariant.Gt, DateTime.UtcNow.AddHours(1).ToString(CultureInfo.InvariantCulture)),
                new FilterLeafDto(nameof(FilteredActionGroupDto.CreatedDate), ComparisonVariant.Lt, DateTime.UtcNow.AddHours(-1).ToString(CultureInfo.InvariantCulture)),
                new FilterLeafDto(nameof(FilteredActionGroupDto.StartedDate), ComparisonVariant.Gt, DateTime.UtcNow.AddHours(1).ToString(CultureInfo.InvariantCulture)),
                new FilterLeafDto(nameof(FilteredActionGroupDto.StartedDate), ComparisonVariant.Lt, DateTime.UtcNow.AddHours(-1).ToString(CultureInfo.InvariantCulture)),
                new FilterLeafDto(nameof(FilteredActionGroupDto.CompletedDate), ComparisonVariant.Gt, DateTime.UtcNow.AddHours(1).ToString(CultureInfo.InvariantCulture)),
                new FilterLeafDto(nameof(FilteredActionGroupDto.CompletedDate), ComparisonVariant.Lt, DateTime.UtcNow.AddHours(-1).ToString(CultureInfo.InvariantCulture))
            ],
            LogicalCombinationVariant.And
        );

        operation.RequestBody.Content["application/json"].Example = example.ToOpenApiObject();
    }
}