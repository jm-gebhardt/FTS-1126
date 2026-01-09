using Gebhardt.Acc.Common.Filter.Api;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.LogEntry;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Gebhardt.Acc.Common.Messages.LogEntry;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using LogEntryDto = Gebhardt.Acc.TransportOrder.WebApi.Dtos.LogEntry.LogEntryDto;

namespace Gebhardt.Acc.TransportOrder.WebApi.Swagger.Examples;

/// <summary>
/// Provides a filter example for filtering log entries.
/// </summary>
[UsedImplicitly]
public class LogEntryFilterExample : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.Name != nameof(LogEntriesController.FilterLogEntries))
        {
            return;
        }
        var example = new CombinationFilterDto(
        [
            new ValueComparisonFilterDto(nameof(LogEntryDto.AgvCorrelationId), ComparisonVariant.Equals, Guid.NewGuid().ToString()),
            new ValueComparisonFilterDto(nameof(LogEntryDto.Category), ComparisonVariant.Equals, nameof(Categories.Agv)),
            new ValueComparisonFilterDto(nameof(LogEntryDto.LogLevel), ComparisonVariant.Equals, nameof(LogLevel.Information))
        ],
        LogicalCombinationVariant.And);
        if (operation.RequestBody?.Content != null)
        {
            operation.RequestBody.Content["application/json"].Example = example.ToOpenApiObject();
        }
    }
}