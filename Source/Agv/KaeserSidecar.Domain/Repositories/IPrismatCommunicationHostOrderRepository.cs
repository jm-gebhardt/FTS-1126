using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Repositories;

namespace Gebhardt.Acc.KaeserSidecar.Domain.Repositories;

/// <summary>
/// Provides an interface for extending the prismat communication host order repository
/// </summary>
public interface IPrismatCommunicationHostOrderRepository : IHostOrderRepository
{
    /// <summary>
    /// Gets the next AKL pick on the short circle (at 1020).
    /// </summary>
    /// <returns></returns>
    public Task<HostOrder?> GetNextPickOnShortCircle();
}
