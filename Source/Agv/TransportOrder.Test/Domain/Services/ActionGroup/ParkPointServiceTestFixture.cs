using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Services.ActionGroup;

public class ParkPointServiceTestFixture
{
    private readonly Mock<INodeRepository> _nodeRepository;

    public ParkPointService ParkPointService { get; }

    public ParkPointServiceTestFixture()
    {
        var agvDb = new Mock<IAgvDb>();
        _nodeRepository = new Mock<INodeRepository>();
        var routeService = new Mock<IRouteService>();
        var logger = new Mock<ILogger<ParkPointService>>();
        var semaphoreProvider = new Mock<ISemaphoreProvider<IParkingSemaphore>>();
        ParkPointService = new ParkPointService(agvDb.Object, _nodeRepository.Object, routeService.Object, logger.Object, semaphoreProvider.Object);
    }

    public ParkPointServiceTestFixture ReserveNodeAsyncReturns(bool returnValue)
    {
        _nodeRepository.Setup(x => x.UpdateLock(It.IsAny<GraphNode>(), It.IsAny<bool>())).ReturnsAsync(returnValue);
        return this;
    }
}