using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.FilterTemplate;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="IOwnedFilterTemplateRepository"/>
public class OwnedFilterTemplateRepository(IDbContextFactory<TransportOrderDbContext> contextFactory) : OwnedResourceRepository<FilterTemplate, OwnedFilterTemplate, TransportOrderDbContext>(contextFactory), IOwnedFilterTemplateRepository;
