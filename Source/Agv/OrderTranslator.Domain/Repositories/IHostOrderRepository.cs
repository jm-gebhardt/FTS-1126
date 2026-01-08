using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.OrderTranslator.Domain.Models.HostOrder;

namespace Gebhardt.Acc.OrderTranslator.Domain.Repositories;

/// <summary>
/// Provides a repository for host order related queries.
/// </summary>
public interface IHostOrderRepository : IRepository<HostOrder>;