using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Time;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Map;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;

public static class SimulatedAgvSagaGenerator
{
    public static SimulatedAgvSaga Generate(
        string agvId = "Agv#1",
        GraphNode? currentNode = null,
        AgvAction? currentAction = null,
        AgvGraph? agvGraph = null,
        AgvData? agvData = null,
        AgvMap? agvMap = null,
        ITimeProvider? timeProvider = null,
        bool graphExists = true,
        bool dataExists = true,
        bool mapExists = true,
        string currentState = nameof(SimulatedAgvStateMachine.Idle),
        int nodeTravelTime = 10,
        int initialSoc = 75,
        double batteryChargeRate = 1.0)
    {
        var correlationId = agvId.ToGuid();
        if (!graphExists)
        {
            agvGraph = null;
        }
        else
        {
            agvGraph ??= AgvGraphGenerator.CreateDefaultGraph(correlationId);
        }
        if (!dataExists)
        {
            agvData = null;
        }
        else
        {
            agvData ??= new AgvData(correlationId, "model", "type", string.Empty, 10.0, 1.0, 1.0, 1.0, 1.0, null, 1.0, 1.0, 42, Array.Empty<AgvData.Sensor>());
        }

        if (!mapExists)
        {
            agvMap = null;
        }
        else
        {
            agvMap ??= agvGraph != null ? new MapGenerator(agvGraph).Generate() : null;
        }
        var agv = new SimulatedAgv(correlationId, agvId, "",
            true,
            false,
            false,
            true,
            DateTime.MinValue,
            DateTime.MinValue,
            AgvGraph: agvGraph, AgvData: agvData, AgvMap: agvMap);
        var simulatedAgvSaga = new SimulatedAgvSaga(agv, nodeTravelTime, initialSoc, batteryChargeRate, timeProvider ?? new DateTimeNowUtcProvider(), currentNode)
        {
            Action = currentAction,
            CurrentState = currentState
        };
        simulatedAgvSaga.UpdatePosition();
        return simulatedAgvSaga;
    }
}