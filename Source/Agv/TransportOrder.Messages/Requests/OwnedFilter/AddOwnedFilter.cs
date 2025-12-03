using Gebhardt.Acc.Common.Filter.Api.Dtos;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;

/// <summary>
/// Represents a request to add an owned filter.
/// </summary>
/// <param name="Owner">The owner of the filter.</param>
/// <param name="Filter">The filter to add.</param>
/// <param name="Name">The name of the filter.</param>
public record AddOwnedFilter(string Owner, FilterDto Filter, string Name);