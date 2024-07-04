using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Time;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation;
using Gebhardt.Acc.DeviceCommunication.SimAgv.States;

namespace Gebhardt.Acc.DeviceCommunicationTest.Helper;

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