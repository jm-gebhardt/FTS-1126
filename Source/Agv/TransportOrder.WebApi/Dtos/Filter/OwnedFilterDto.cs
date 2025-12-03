using Gebhardt.Acc.Common.Filter.Api.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Filter;

/// <summary>
/// Data transfer object of owned filters.
/// </summary>
/// <param name="CorrelationId">The id of the owned filter.</param>
/// <param name="Filter">The actual filter.</param>
/// <param name="Name">The name of the filter.</param>
[UsedImplicitly]
public record OwnedFilterDto(Guid CorrelationId, FilterDto Filter, string Name)
{
    /// <summary>
    /// Automapper ctor
    /// </summary>
    private OwnedFilterDto() : this(Guid.Empty, null!, "")
    {
    }
}