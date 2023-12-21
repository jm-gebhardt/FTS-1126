using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation;

/// <summary>
/// SimulatedAgvProviderTest
/// </summary>
[TestClass]
public class SimulatedAgvProviderTest
{
    private static SimulateOptions GetSimulatedOptions(int numberOfAgvs = 1)
    {
        return new SimulateOptions()
        {
            Agvs = numberOfAgvs,
            Sources = new[] {"S1"},
            Destinations = new[] {"D1"},
            GridRows = 3,
            GridGraph = true
        };
    }

    [TestMethod]
    public async Task SimulatedAgvProvider_Create_Triggered_CreateAgv_Published()
    {
        var options = GetSimulatedOptions();

        StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga> fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                s =>
                {
                    s.AddSingleton(options);
                    s.AddLogging();
                    s.AddTransient<IGraphGenerator, GridGraphGenerator>(_ => new GridGraphGenerator(options));
                    s.AddHostedService<SimulatedAgvProvider>();
                },
                inactivityTimeout: TimeSpan.FromSeconds(6))
            .Build();

        (fixture.Provider.GetRequiredService<IHostedService>() as SimulatedAgvProvider).ShouldNotBeNull();
        bool result = await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.CreateAgv>();
        result.ShouldBeTrue();
    }

    [TestMethod]
    public async Task TwoAgvs_SingleSourceAndDestination_CreateAgvPublished_AgvIdUnique_Successful()
    {
        var options = GetSimulatedOptions(2);

        StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga> fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                s =>
                {
                    s.AddSingleton(options);
                    s.AddLogging();
                    s.AddTransient<IGraphGenerator, GridGraphGenerator>(_ => new GridGraphGenerator(options));
                    s.AddHostedService<SimulatedAgvProvider>();
                },
                inactivityTimeout: TimeSpan.FromSeconds(6),
                inMemoryConfiguration: (_, cfg) => { cfg.UseDelayedMessageScheduler(); },
                busRegistrationConfigurator:
                x => { x.AddDelayedMessageScheduler(); })
            .Build();

        (fixture.Provider.GetRequiredService<IHostedService>() as SimulatedAgvProvider).ShouldNotBeNull();

        (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.CreateAgv>()).ShouldBeTrue();

        for (int i = 0; i < options.Agvs; i++)
        {
            int i1 = i;
            var createdSaga = fixture.SagaHarness.Sagas.Select(c => c.CorrelationId == $"SimAgv-{i1}".ToGuid()).First().Saga;

            createdSaga.CorrelationId.ShouldBe($"SimAgv-{i}".ToGuid());
            createdSaga.AgvId.ShouldBe($"SimAgv-{i}");
            createdSaga.AgvStatus.ShouldNotBeNull();
            createdSaga.AgvGraph.ShouldNotBeNull();
            createdSaga.AgvData.ShouldNotBeNull();

            (await fixture.TestHarness.Consumed.Any<SimulatedAgvStateMachine.CreateAgvResponse>(c => c.Context.CorrelationId == $"SimAgv-{i}".ToGuid())).ShouldBeTrue();
        }
    }
}