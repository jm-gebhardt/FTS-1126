using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;

/// <summary>
/// Provides on-the-fly generation of <see cref="AgvGraph"/> from stored <see cref="GraphLayout"/> data
/// filtered by an AGV's vehicle type.
/// </summary>
[UsedImplicitly]
public class AgvGraphProvider(
    IGraphLayoutRepository graphLayoutRepository,
    IAgvRepository agvRepository,
    ILogger<AgvGraphProvider> logger) : IAgvGraphProvider
{
    /// <inheritdoc />
    public async Task<AgvGraph?> GetAgvGraphAsync(Guid agvCorrelationId)
    {
        string agvVehicleTypeId = (await agvRepository.GetRequiredAgvAsync(agvCorrelationId)).VehicleTypeId;
        ICollection<GraphLayout> layouts = await graphLayoutRepository.GetLayoutsForVehicleTypeAsync(agvVehicleTypeId);

        if (layouts.Count == 0)
        {
            logger.LogDebug("No stored graph layouts for VehicleTypeId '{VehicleTypeId}' of AGV {AgvCorrelationId}",
                agvVehicleTypeId, agvCorrelationId);
            return null;
        }

        return layouts.ToAgvGraph(agvVehicleTypeId, agvCorrelationId);
    }

    /// <inheritdoc />
    public async Task<AgvGraph> GetRequiredAgvGraphAsync(Guid agvCorrelationId)
    {
        return await GetAgvGraphAsync(agvCorrelationId) ?? throw new AgvGraphNotFoundException(agvCorrelationId);
    }
}