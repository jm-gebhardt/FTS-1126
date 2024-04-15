using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Time;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;

public static class SimulatedAgvSagaGenerator
{
    public static SimulatedAgvSaga Generate(
        string agvSerialNumber = "Agv#1",
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
        var correlationId = agvSerialNumber.ToGuid();
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
            agvData ??= AgvDataGenerator.GenerateAgvData();
        }

        var agv = new SimulatedAgv(correlationId, agvSerialNumber, "",
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