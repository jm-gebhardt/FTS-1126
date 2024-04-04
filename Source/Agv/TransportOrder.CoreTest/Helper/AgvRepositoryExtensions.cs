using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Settings;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class AgvRepositoryExtensions
{
    public static void AddAgvsToMockRepository(this Mock<IAgvDb> repoMock, List<TransportOrder.Domain.Models.Agv.Agv> agvs) => AddAgvsToMockRepository(repoMock, agvs.ToArray());

    public static void AddAgvsToMockRepository(this Mock<IAgvDb> repoMock, params TransportOrder.Domain.Models.Agv.Agv[] agvs)
    {
        repoMock.Setup(s => s.Agvs.GetAllAgvsAsync()).ReturnsAsync(agvs);

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
        repoMock.Setup(r => r.Agvs.TryGetAgvAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => { return agvs.FirstOrDefault(a => a.CorrelationId == c); });

        repoMock.Setup(r => r.Agvs.Count).Returns(agvs.Count);
        repoMock.Setup(r => r.Agvs.CheckIfAgvExistsAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => agvs.Any(a => a.CorrelationId == c));
        repoMock.Setup(r => r.Agvs.GetAllAgvsWithGraphAsync()).ReturnsAsync(agvs.Where(a => a.GraphExists).ToList());
        repoMock.Setup(r => r.Agvs.GetChargingAgvsWithMinBatteryLevelAsync()).ReturnsAsync(() => agvs);
        repoMock.Setup(r => r.Agvs.GetIdleAgvsAsync()).ReturnsAsync(agvs.Where(a => a.CurrentAgvState == AgvState.Idle).ToList());
        repoMock.Setup(r => r.Agvs.GetReadyAgvsAsync(It.IsAny<int>())).ReturnsAsync((int minBatteryLevel) => agvs.Where(a => a.IsReady && a.AgvStatus!.BatteryCharge >= minBatteryLevel).ToList());
        repoMock.Setup(r => r.Agvs.SelectExistingAgvsAsync(It.IsAny<List<Guid>>())).ReturnsAsync((List<Guid> cs) => agvs.Where(a => !cs.Any() || cs.Contains(a.CorrelationId)).ToList());
        AddNonExistingAgvs(repoMock, agvs.Select(a => a.CorrelationId).ToArray());
        repoMock.Setup(r => r.AgvSettings.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(EfAgvSettingsProvider.DefaultSettings);
        repoMock.Setup(r => r.AgvSettings.GetSettingsGroupForAgv(It.IsAny<TransportOrder.Domain.Models.Agv.Agv?>())).ReturnsAsync(new AgvSettingsGroup(GroupType.All));
    }

    private static void AddNonExistingAgvs(this Mock<IAgvDb> repoMock, params Guid[] agvCorrelationIds)
    {
        repoMock.Setup(r => r.Agvs.ThrowIfAgvNotExistsAsync(It.IsAny<Guid>())).Callback((Guid correlationId) =>
        {
            if (agvCorrelationIds.All(a => a != correlationId))
            {
                throw new AgvNotFoundException(correlationId);
            }
        });
        repoMock.Setup(r => r.Agvs.ThrowIfAgvsNotExistAsync(It.IsAny<ICollection<Guid>>())).Callback((ICollection<Guid> correlationIds) =>
        {
            List<Guid> notExistingAgvs = correlationIds.Except(agvCorrelationIds).ToList();
            if (notExistingAgvs.Any())
            {
                throw new AgvsNotFoundException(notExistingAgvs);
            }
        });
    }
}