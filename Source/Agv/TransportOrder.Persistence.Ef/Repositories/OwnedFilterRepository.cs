using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.TransportOrder.Domain.Models.Filter;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <inheritdoc cref="IOwnedFilterRepository"/>
public class OwnedFilterRepository(IDbContextFactory<TransportOrderDbContext> contextFactory) : OwnedResourceRepository<FilterDto, OwnedFilter, TransportOrderDbContext>(contextFactory), IOwnedFilterRepository;