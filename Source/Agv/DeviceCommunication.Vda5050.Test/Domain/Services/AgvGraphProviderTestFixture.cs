using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

internal class AgvGraphProviderTestFixture : FixtureBase
{
    private readonly Mock<IGraphLayoutRepository> _graphLayoutRepository = new();

    private readonly Mock<IAgvRepository> _agvRepository = new();

    public AgvGraphProvider GraphProvider { get; }

    public AgvGraphProviderTestFixture()
    {
        GraphProvider = new AgvGraphProvider(
            _graphLayoutRepository.Object,
            _agvRepository.Object,
            Mock.Of<ILogger<AgvGraphProvider>>());
    }

    public AgvGraphProviderTestFixture SetAgv(Agv agv)
    {
        _agvRepository.Setup(x => x.GetRequiredAgvAsync(agv.CorrelationId)).ReturnsAsync(agv);
        return this;
    }

    public AgvGraphProviderTestFixture SetLayouts(string vehicleTypeId, ICollection<GraphLayout> layouts)
    {
        _graphLayoutRepository.Setup(x => x.GetLayoutsForVehicleTypeAsync(vehicleTypeId)).ReturnsAsync(layouts);
        return this;
    }
}