using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.Routing;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Domain.Services.ActionGroup;

public class ParkPointServiceTestFixture
{
    private readonly Mock<INodeStateRepository> _nodeStateRepository;

    public ParkPointService ParkPointService { get; }

    public ParkPointServiceTestFixture()
    {
        var agvDb = new Mock<IAgvDb>();
        _nodeStateRepository = new Mock<INodeStateRepository>();
        var routeService = new Mock<IRouteService>();
        var logger = new Mock<ILogger<ParkPointService>>();
        var semaphoreProvider = new Mock<ISemaphoreProvider<IParkingSemaphore>>();
        ParkPointService = new ParkPointService(agvDb.Object, _nodeStateRepository.Object, routeService.Object, logger.Object, semaphoreProvider.Object);
    }

    public ParkPointServiceTestFixture ReserveNodeAsyncReturns(bool returnValue)
    {
        _nodeStateRepository.Setup(x => x.ReserveNodeAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(returnValue);
        return this;
    }
}