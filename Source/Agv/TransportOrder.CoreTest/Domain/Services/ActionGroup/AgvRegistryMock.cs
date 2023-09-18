using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Domain.Services.ActionGroup;

public static class AgvRegistryMock
{
    private static readonly string[] _orders = { "ORDER001", "ORDER002", "ORDER003" };
    private static readonly string[] _sources = { "SRC1", "SRC2", "SRC3" };
    private static readonly string[] _agvIds = { "AGV001", "AGV002", "AGV003" };
    private static readonly string[] _agvStates = { "Ready", "Busy", "Idle" };
    
    public static Mock<IAgvRegistry> GetAgvRegistry()
    {
        var registry = new Mock<IAgvRegistry>();

        var agvs = new List<IAgv>();
        for (int i = 0; i < _agvIds.Length; i++)
        {
            string agvId = _agvIds[i];
            string order = _orders[i];
            string state = _agvStates[i];
            var correlationId = agvId.ToGuid();

            var agvStatus = new AgvStatus(correlationId, order, true, true, 10, false, false, _sources[i]);

            agvs.Add(new AgvSaga(correlationId, agvId, state, agvStatus) { IsOnline = true, GraphExists = true });
        }

        registry.SetupGet(x => x.Count).Returns(agvs.Count);
        registry.SetupGet(x => x.CountIdle).Returns(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle)).Count);
        registry.Setup(x => x.GetIdleAgvs()).Returns(new EnumerableQuery<IAgv>(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle))));
        registry.Setup(x => x.GetAllAgvsWithGraphAsync()).ReturnsAsync(new List<IAgv>(agvs));
        registry.Setup(x => x.GetAllAgvs()).Returns(new EnumerableQuery<IAgv>(agvs));
        registry.Setup(x => x.GetAgv(It.IsAny<Guid>())).Returns<Guid>(correlationId =>
        {
            return agvs.FirstOrDefault(agv => agv.CorrelationId == correlationId);
        });
        return registry;
    }
}