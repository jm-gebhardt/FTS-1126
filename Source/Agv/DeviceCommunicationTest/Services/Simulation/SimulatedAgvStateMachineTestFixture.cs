using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.SimAgv.CmdOptions;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Maps;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Travel;
using Gebhardt.Acc.DeviceCommunication.SimAgv.States;
using Gebhardt.Acc.DeviceCommunicationTest.Helper;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

public class SimulatedAgvStateMachineTestFixture(TimeSpan? inactivityTimeout = null) : BaseStateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(inactivityTimeout: inactivityTimeout)
{
    private Options _options = SimulateOptionsGenerator.GenerateSimulateOptions("S1", "D1", nodeTravelTime: 500);
    private readonly List<SimulatedAgvSaga> _agvSagas = new();

    public new SimulatedAgvStateMachineTestFixture Build()
    {
        AddServices(services =>
        {
            services.AddSingleton(_ => _options);
            services.AddLogging();
            services.AddTransient<IGraphGenerator, GridGraphGenerator>(_ => new GridGraphGenerator(_options, new GeneratedMapGenerator()));
        });

        base.Build();

        foreach (var agvSaga in _agvSagas)
        {
            Add(agvSaga);
        }
        
        Machine.SignalPositionIntervalMs = 500;
        Machine.StatusUpdateIntervalMs = 50;

        return this;
    }

    public SimulatedAgvStateMachineTestFixture WithSimulateOptions(Options options)
    {
        _options = options;
        return this;
    }

    public async Task WaitUntilAgvIdle(SimulatedAgvSaga agvSaga, int timeoutInMs = 5000)
    {
        await WaitUntil(agvSaga, s => !s.AgvStatus.Busy, s => s.AgvStatus.Busy.ShouldBeFalse(), timeoutInMs);
    }

    public async Task WaitUntil(SimulatedAgvSaga agvSaga, Func<SimulatedAgvSaga, bool> conditionGood, Action<SimulatedAgvSaga> finalCheck, int timeoutInMs = 5000)
    {
        var startedTime = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(timeoutInMs);
        bool conditionFulfilled;
        do
        {
            conditionFulfilled = conditionGood(agvSaga);
            await Task.Delay(1);
        } while (!conditionFulfilled && DateTime.UtcNow < startedTime + timeout);
        finalCheck(agvSaga);
    }

    public SimulatedAgvStateMachineTestFixture WithAgv(SimulatedAgvSaga agvSaga)
    {
        _agvSagas.Add(agvSaga);
        return this;
    }

    public SimulatedAgvStateMachineTestFixture WithAgvTravelSimulator(IAgvTravelSimulator agvTravelSimulator)
    {
        foreach (var agvSaga in _agvSagas)
        {
            agvSaga.TravelSimulator = agvTravelSimulator;
        }

        return this;
    }
}