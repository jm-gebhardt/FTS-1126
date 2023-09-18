using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;

public static class AgvSagaGenerator
{
    /// <summary>
    /// Create a standardized AgvSaga for testing purposes
    /// </summary>
    /// <param name="state"></param>
    /// <param name="correlationId"></param>
    /// <param name="hasGraph"></param>
    public static AgvSaga GenerateAgvSaga(string state, Guid? correlationId = null, bool hasGraph = true)
    {
        correlationId ??= Guid.NewGuid();
        string agvId = correlationId.ToString()!;

        var agvGraph = hasGraph ? AgvGraphGenerator.CreateDefaultGraph() : null;
        var agvStatus = new AgvStatus(
            correlationId.Value,
            null,
            true,
            true,
            10,
            true,
            true,
            lastNodeId: agvGraph?.Nodes.First().Id
        );

        var agvSaga = new AgvSaga(
            correlationId.Value,
            agvId,
            currentState: state,
            agvStatus: agvStatus
        );
        return agvSaga;
    }
}