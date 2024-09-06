using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Moq;
using IAgvRepository = Gebhardt.Acc.TransportOrder.Domain.Repositories.IAgvRepository;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class MockAgvRepositoryExtensions
{
    public static void AddAgvs(this Mock<IAgvRepository> repoMock, params Agv[] agvs)
    {
        repoMock.Setup(s => s.GetAllAgvsAsync()).ReturnsAsync(agvs);

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
        repoMock.Setup(r => r.TryGetAgvAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => { return agvs.FirstOrDefault(a => a.CorrelationId == c); });

        repoMock.Setup(r => r.Count).Returns(agvs.Count);
        repoMock.Setup(r => r.CheckIfAgvExistsAsync(It.IsAny<Guid>())).ReturnsAsync((Guid c) => agvs.Any(a => a.CorrelationId == c));
        repoMock.Setup(r => r.GetAllAgvsWithGraphAsync()).ReturnsAsync(agvs.Where(a => a.GraphExists).ToList());
        repoMock.Setup(r => r.GetChargingAgvsWithMinBatteryLevelAsync()).ReturnsAsync( agvs);
        repoMock.Setup(r => r.GetIdleAgvsAsync()).ReturnsAsync(agvs.Where(a => a.CurrentAgvState == AgvState.Idle).ToList());
        repoMock.Setup(r => r.SelectExistingAgvsAsync(It.IsAny<List<Guid>>())).ReturnsAsync((List<Guid> cs) => agvs.Where(a => !cs.Any() || cs.Contains(a.CorrelationId)).ToList());
        AddNonExistingAgvs(repoMock, agvs.Select(a => a.CorrelationId).ToArray());
    }
    
    private static void AddNonExistingAgvs(this Mock<IAgvRepository> repoMock, params Guid[] agvCorrelationIds)
    {
        repoMock.Setup(r => r.ThrowIfAgvNotExistsAsync(It.IsAny<Guid>())).Callback((Guid correlationId) =>
        {
            if (agvCorrelationIds.All(a => a != correlationId))
            {
                throw new AgvNotFoundException(correlationId);
            }
        });
        repoMock.Setup(r => r.ThrowIfAgvsNotExistAsync(It.IsAny<ICollection<Guid>>())).Callback((ICollection<Guid> correlationIds) =>
        {
            List<Guid> notExistingAgvs = correlationIds.Except(agvCorrelationIds).ToList();
            if (notExistingAgvs.Any())
            {
                throw new AgvsNotFoundException(notExistingAgvs);
            }
        });
    }
}