using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Persistence.Ef.Repositories;

/// <inheritdoc/>
public class EfAgvGraphRepository: IAgvGraphRepository
{
    /// <inheritdoc/>
    public Task<AgvGraph> GetAgvGraphAsync(Guid agvCorrelationId)
    {
        throw new NotImplementedException();
    }
}