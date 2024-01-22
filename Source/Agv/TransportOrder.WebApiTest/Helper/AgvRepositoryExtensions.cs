using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Helper;

public static class AgvRepositoryExtensions
{
    public static void AddAgvsToMockRepository(this Mock<IAgvDb> repoMock, IList<Domain.Models.Agv.Agv> agvs)
    {
        repoMock.Setup(s => s.Agvs.GetAllAgvsAsync()).ReturnsAsync(agvs.ToList());

        repoMock.Setup(r => r.Agvs.GetAgvAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) =>
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

        repoMock.Setup(r => r.Agvs.Count).Returns(agvs.Count);
        repoMock.Setup(r => r.Agvs.CheckIfAgvExistsAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => agvs.Any(a => a.CorrelationId == c));
        repoMock.Setup(r => r.Agvs.GetAllAgvsWithGraphAsync()).ReturnsAsync(agvs.Where(a => a.GraphExists).ToList());
        repoMock.Setup(r => r.Agvs.GetChargingAgvsWithMinBatteryLevelAsync(It.IsAny<int>())).ReturnsAsync((int bl) => agvs.Where(a => a.AgvStatus?.BatteryCharge > bl).ToList());
        repoMock.Setup(r => r.Agvs.GetIdleAgvsAsync()).ReturnsAsync(agvs.Where(a => a.CurrentAgvState == AgvState.Idle).ToList());
        repoMock.Setup(r => r.Agvs.GetReadyAgvsAsync(It.IsAny<int>())).ReturnsAsync((int minBatteryLevel) => agvs.Where(a => a.IsReady && a.AgvStatus!.BatteryCharge >= minBatteryLevel).ToList());
        repoMock.Setup(r => r.Agvs.SelectExistingAgvsAsync(It.IsAny<List<Guid>>())).ReturnsAsync((List<Guid> cs) => agvs.Where(a => !cs.Any() || cs.Contains(a.CorrelationId)).ToList());
    }
}