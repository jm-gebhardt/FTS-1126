using Moq;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.Agvs;

public class AgvReadyServiceTestFixture
{
    private Mock<IAgvGraphRegistry> AgvGraphRegistry { get; } = new();
    public AgvReadyServiceTestFixture()
    {
        AgvGraphRegistry.Setup(x => x.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        AgvReadyService = new AgvReadyService(AgvGraphRegistry.Object, new Mock<ILogger<AgvReadyService>>().Object);
    }

    public AgvReadyService AgvReadyService { get; }

    public AgvReadyServiceTestFixture WithoutExistingAgvGraph(Guid correlationId)
    {
        AgvGraphRegistry.Setup(x => x.ExistsAsync(correlationId)).ReturnsAsync(false);
        return this;
    }
}