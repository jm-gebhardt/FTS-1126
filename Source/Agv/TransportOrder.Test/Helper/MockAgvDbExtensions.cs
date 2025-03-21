using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class MockAgvDbExtensions
{
    public static void AddAgvs(this Mock<IAgvDb> repoMock, List<Agv> agvs) => AddAgvs(repoMock, agvs.ToArray());

    public static void AddAgvs(this Mock<IAgvDb> repoMock, params Agv[] agvs)
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
        repoMock.Setup(r => r.Agvs.GetChargingAgvsWithMinBatteryLevelAsync()).ReturnsAsync(agvs);
        repoMock.Setup(r => r.Agvs.GetIdleAgvsAsync()).ReturnsAsync(agvs.Where(a => a.CurrentAgvState == AgvState.Idle).ToList());
        repoMock.Setup(r => r.Agvs.SelectExistingAgvsAsync(It.IsAny<List<Guid>>())).ReturnsAsync((List<Guid> cs) => agvs.Where(a => !cs.Any() || cs.Contains(a.CorrelationId)).ToList());
        AddNonExistingAgvs(repoMock, agvs.Select(a => a.CorrelationId).ToArray());
        repoMock.Setup(r => r.AgvSettings.GetSettingsForAgvGroup(It.IsAny<Guid>())).Returns(EfAgvSettingsProvider.DefaultSettings);
        repoMock.Setup(r => r.AgvSettings.GetSettingsGroupForAgv(It.IsAny<Agv?>())).ReturnsAsync(new AgvSettingsGroup(GroupType.All));
        repoMock.Setup(r => r.Agvs.GetReadyAgvsWithAtLeastLowBatteryLevel()).ReturnsAsync(agvs);
        repoMock.Setup(r => r.Agvs.SelectAgvsWithAboveCriticalBatteryLevel(It.IsAny<ICollection<Guid>>())).ReturnsAsync(agvs);
        repoMock.Setup(r => r.Agvs.HasOnlineAgvs()).ReturnsAsync(true);
        repoMock.Setup(x => x.Positions.GetPosition(It.IsAny<Guid>())).Returns((Guid c) =>
        {
            var agv = agvs.FirstOrDefault(a => a.CorrelationId == c);
            return agv == null ? null : new AgvPositionStatus(agv.CorrelationId, agv.SerialNumber, new AgvPosition(true, 1, 1, 0, 0));
        });
        repoMock.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync([]);
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

    public static void AddZones(this Mock<IAgvDb> repoMock, ICollection<Zone> zones)
    {
        repoMock.Setup(r => r.Zones.GetZones(It.IsAny<string[]>())).ReturnsAsync(zones);
    }

    public static void AddPosition(this Mock<IAgvDb> repoMock, AgvPositionStatus position)
    {
        repoMock.Setup(x => x.Positions.GetPosition(It.IsAny<Guid>())).Returns(position);
    }

    public static void AddAgvGraph(this Mock<IAgvDb> repoMock, AgvGraph? agvGraph = null, bool graphExists = true)
    {
        repoMock.Setup(x => x.Graphs.GetAgvGraphAsync(It.IsAny<Guid>()))!.ReturnsAsync(agvGraph);
        repoMock.Setup(x => x.Graphs.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(graphExists);
        repoMock.Setup(x => x.Graphs.ExistsAsync()).ReturnsAsync(graphExists);
    }

    public static void AddNode(this Mock<IAgvDb> repoMock, GraphNode? node)
    {
        repoMock.Setup(x => x.Graphs.TryGetNodeAsync(It.IsAny<string>())).ReturnsAsync(node);
    }
    
    public static void AddAgvData(this Mock<IAgvDb> repoMock, AgvData? agvData)
    {
        repoMock.Setup(x => x.Data.GetSelectedAgvDataAsync(It.IsAny<ICollection<Guid>>())).ReturnsAsync([agvData!]);
        repoMock.Setup(x => x.Data.GetAgvDataAsync(It.IsAny<Guid>())).ReturnsAsync(agvData);
        repoMock.Setup(x => x.Data.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
    }
    
    public static void AddAgvData(this Mock<IAgvDb> repoMock, Func<Guid, AgvData?> agvData)
    {
        repoMock.Setup(x => x.Data.GetAgvDataAsync(It.IsAny<Guid>())).ReturnsAsync(agvData);
        repoMock.Setup(x => x.Data.ExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
    }

    public static void AddActiveErrors(this Mock<IAgvDb> repoMock, IList<AgvError> errors)
    {
        repoMock.Setup(x => x.Errors.GetActiveErrorsAsync(It.IsAny<Guid>())).ReturnsAsync(errors);
    }
}