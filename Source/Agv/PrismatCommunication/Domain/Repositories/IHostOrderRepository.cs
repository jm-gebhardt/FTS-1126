using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Repositories;

/// <summary>
/// Provides a repository for host order related queries.
/// </summary>
public interface IHostOrderRepository : IRepository<HostOrder>
{
    /// <summary>
    /// Update an order.
    /// </summary>
    /// <param name="hostOrder">The host order.</param>
    Task UpdateAsync(HostOrder hostOrder);
}