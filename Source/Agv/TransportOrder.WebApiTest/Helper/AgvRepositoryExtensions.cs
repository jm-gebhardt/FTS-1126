using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Helper;

public static class AgvRepositoryExtensions
{
    public static void AddAgvsToMockRepository(this Mock<IAgvRepository> repoMock, IList<Domain.Models.Agv.Agv> agvs)
    {
        repoMock.Setup(s => s.GetAllAgvsAsync()).ReturnsAsync(agvs.ToList());

        repoMock.Setup(r => r.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) =>
        {
            try
            {
                return agvs.First(a => a.CorrelationId == c);
            }
            catch (InvalidOperationException)
            {
                throw new AgvNotFoundException(c);
            }
        });

        repoMock.Setup(r => r.Count).Returns(agvs.Count);
        repoMock.Setup(r => r.CheckIfAgvExistsAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => agvs.Any(a => a.CorrelationId == c));
        repoMock.Setup(r => r.GetAllAgvsWithGraphAsync()).ReturnsAsync(agvs.Where(a => a.GraphExists).ToList());
        repoMock.Setup(r => r.GetChargingAgvsWithMinBatteryLevelAsync(It.IsAny<int>())).ReturnsAsync((int bl) => agvs.Where(a => a.AgvStatus?.BatteryCharge > bl).ToList());
        repoMock.Setup(r => r.GetIdleAgvsAsync()).ReturnsAsync(agvs.Where(a => a.CurrentAgvState == AgvState.Idle).ToList());
        repoMock.Setup(r => r.GetReadyAgvsAsync()).ReturnsAsync(agvs.Where(a => a.IsReady).ToList());
        repoMock.Setup(r => r.SelectExistingAgvsAsync(It.IsAny<List<Guid>>())).ReturnsAsync((List<Guid> cs) => agvs.Where(a => !cs.Any() || cs.Contains(a.CorrelationId)).ToList());
    }
}