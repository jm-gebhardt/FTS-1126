using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation
{
    /// <summary>
    /// SimulatedAgvProviderTest
    /// </summary>
    [TestClass]
    public class SimulatedAgvProviderTest
    {
        [TestMethod]
        public async Task SimulatedAgvProvider_Create_Triggered_CreateAgv_Published()
        {
            var options = new SimulateOptions()
            {
                Agvs = 1,
                Sources = new[] { "S1" },
                Destinations = new[] { "D1" },
                GridRows = 3
            };

            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                services: s =>
                {
                    s.AddSingleton(options);
                    s.AddLogging();
                    s.AddHostedService<SimulatedAgvProvider>();
                });

            (fixture.Provider.GetRequiredService<IHostedService>() as SimulatedAgvProvider).ShouldNotBeNull();
            bool result = await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.CreateAgv>();
            result.ShouldBeTrue();
        }

        [TestMethod]
        public async Task TwoAgvs_SingleSourceAndDestination_CreateAgvPublished_AgvIdUnique_Successful()
        {
            const int numberOfAgvs = 2;
            var options = new SimulateOptions()
            {
                Agvs = numberOfAgvs,
                Sources = new[] { "S1" },
                Destinations = new[] { "D1" },
                GridRows = 3
            };

            var fixture = new StateMachineTestFixture<SimulatedAgvStateMachine, SimulatedAgvSaga>(
                services: s =>
                {
                    s.AddSingleton(options);
                    s.AddLogging();
                    s.AddHostedService<SimulatedAgvProvider>();
                },
                inMemoryConfiguration: (_, cfg) => { cfg.UseDelayedMessageScheduler(); },
                busRegistrationConfigurator:
                x =>
                {
                    x.AddDelayedMessageScheduler();
                    x.AddHostedService<SimulatedAgvProvider>();
                });
            
            (await fixture.TestHarness.Published.Any<SimulatedAgvStateMachine.CreateAgv>()).ShouldBeTrue();
            
            (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.CreateAgv>()).ShouldBeTrue();
            
            const string agvPrefix = "SimAgv-";

            for (int i = 0; i < numberOfAgvs; i++)
            {
                int i1 = i;
                var createdSaga = fixture.SagaHarness.Sagas.Select(c => c.CorrelationId == $"{agvPrefix}{i1}".ToGuid()).First().Saga;

                createdSaga.CorrelationId.ShouldBe($"{agvPrefix}{i}".ToGuid());
                createdSaga.AgvId.ShouldBe($"{agvPrefix}{i}");
                createdSaga.AgvStatus.ShouldNotBeNull();
                createdSaga.AgvGraph.ShouldNotBeNull();
                createdSaga.AgvData.ShouldNotBeNull();

                (await fixture.TestHarness.Consumed.Any<SimulatedAgvStateMachine.CreateAgvResponse>(c => c.Context.CorrelationId == $"{agvPrefix}{i}".ToGuid())).ShouldBeTrue();
            }
        }
    }
}