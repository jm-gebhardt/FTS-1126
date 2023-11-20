using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation;

public class SimulatedAgvStateMachineTestFixture : StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>
{
    private SimulateOptions? _simulateOptions;
    private readonly List<SimulatedAgvSaga> _agvSagas = new();
    private int _signalPositionIntervalMs;

    public SimulatedAgvStateMachineTestFixture(TimeSpan? inactivityTimeout = null) : base(
        _ => { },
        x => x.AddDelayedMessageScheduler(),
        (_, cfg) => cfg.UseDelayedMessageScheduler(),
        inactivityTimeout: inactivityTimeout)
    {
    }

    public SimulatedAgvStateMachineTestFixture WithSignalPositionIntervalMs(int signalPositionIntervalMs)
    {
        _signalPositionIntervalMs = signalPositionIntervalMs;
        return this;
    }

    public new SimulatedAgvStateMachineTestFixture Build()
    {
        _simulateOptions ??= SimulateOptionsGenerator.GenerateSimulateOptions("S1", "D1", nodeTravelTime: 500);
        AddServices(services => services.AddSingleton(_ => _simulateOptions));

        base.Build();
        foreach (var agvSaga in _agvSagas)
        {
            Add(agvSaga);
        }
        Machine.SignalPositionIntervalMs = _signalPositionIntervalMs;
        Machine.StatusUpdateIntervalMs = 50;

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
}