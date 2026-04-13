using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <inheritdoc/>
public interface IOwnedFilterTemplateRepository : IOwnedResourceRepository<FilterTemplate, OwnedFilterTemplate>;
