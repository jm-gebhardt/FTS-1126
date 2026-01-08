using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.OrderTranslator.Domain.Models.HostOrder;
using Gebhardt.Acc.OrderTranslator.Domain.Repositories;
using Gebhardt.Acc.OrderTranslator.Persistence.Ef.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.OrderTranslator.Persistence.Ef.Repositories;

/// <inheritdoc cref="IHostOrderRepository"/>
public class EfHostOrderRepository(IDbContextFactory<OrderTranslatorDbContext> contextFactory) : EfRepository<HostOrder, OrderTranslatorDbContext, IDbContextFactory<OrderTranslatorDbContext>>(contextFactory), IHostOrderRepository;
