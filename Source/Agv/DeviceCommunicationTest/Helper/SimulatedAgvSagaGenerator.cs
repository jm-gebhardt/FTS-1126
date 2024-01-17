using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Time;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
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
        ITimeProvider? timeProvider = null,
        bool graphExists = true,
        bool dataExists = true,
        string currentState = nameof(SimulatedAgvStateMachine.Idle),
        int simulationSpeed = 1,
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
            agvData ??= new AgvData(correlationId, "model", "type", string.Empty, 10.0, 1.0, 1.0, 1.0, 1.0, 0.08726646, 1.0, 1.0, 42, Array.Empty<AgvData.Sensor>());
        }

        var agv = new SimulatedAgv(correlationId, agvId, "",
            true,
            false,
            false,
            true,
            DateTime.MinValue,
            DateTime.MinValue,
            AgvGraph: agvGraph, AgvData: agvData);
        var simulatedAgvSaga = new SimulatedAgvSaga(agv, simulationSpeed, initialSoc, batteryChargeRate, timeProvider ?? new DateTimeNowUtcProvider(), currentNode)
        {
            Action = currentAction,
            CurrentState = currentState
        };
        simulatedAgvSaga.UpdatePosition();
        return simulatedAgvSaga;
    }
}