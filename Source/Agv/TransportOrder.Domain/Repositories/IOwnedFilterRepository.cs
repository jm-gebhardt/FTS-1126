using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.TransportOrder.Domain.Models.Filter;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <inheritdoc/>
public interface IOwnedFilterRepository : IOwnedResourceRepository<FilterDto, OwnedFilter>;