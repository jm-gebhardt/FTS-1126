using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

public class AgvRouteServiceTestFixture : DbTestFixtureBase<TransportOrderDbContext>
{
    private readonly Mock<IAgvRegistry> _agvRegistryMock = new();
    private readonly Mock<ILogger<AgvRouteRetrievalRetrievalService>> _loggerMock = new();
    private readonly List<ActionGroup> _actionsGroups = new();
    private readonly List<IAgv> _agvs = new();

    public AgvRouteRetrievalRetrievalService AgvRouteRetrievalRetrievalService { get; }

    public AgvRouteServiceTestFixture()
    {
        AgvRouteRetrievalRetrievalService = new AgvRouteRetrievalRetrievalService(ContextFactory, _agvRegistryMock.Object, _loggerMock.Object);
    }

    private void SetupAgvs()
    {
        foreach (var agv in _agvs)
        {
            _agvRegistryMock.Setup(x => x.GetLastNodeIdAsync(agv.CorrelationId)).ReturnsAsync(agv.AgvStatus!.LastNodeId);
        }

        _agvRegistryMock.Setup(x => x.GetLastNodeIdAsync(It.IsAny<ICollection<Guid>>()))
            .Returns<ICollection<Guid>>(agvCorrelationIds =>
            {
                var result = new Dictionary<Guid, string?>();
                foreach (var id in agvCorrelationIds)
                {
                    var matchingAgv = _agvs.FirstOrDefault(agv => agv.CorrelationId == id);
                    if (matchingAgv != null)
                    {
                        result[id] = matchingAgv.AgvStatus?.LastNodeId;
                    }
                }
                return Task.FromResult((IReadOnlyDictionary<Guid, string?>) result);
            });
    }

    public AgvRouteServiceTestFixture WithAgv(IAgv agv)
    {
        _agvs.Add(agv);
        return this;
    }

    public AgvRouteServiceTestFixture WithActionGroup(ActionGroup actionGroup)
    {
        _actionsGroups.Add(actionGroup);
        return this;
    }

    public async Task<AgvRouteServiceTestFixture> BuildAsync()
    {
        SetupAgvs();

        await AddRangeAsync(_actionsGroups);
        return this;
    }
}