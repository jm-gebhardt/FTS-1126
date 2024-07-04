using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.SimAgv.CmdOptions;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

/// <summary>
/// SimulatedAgvProviderTest
/// </summary>
[TestClass]
public class SimulatedAgvProviderTest
{
    private static Options GetSimulatedOptions(int numberOfAgvs = 1)
    {
        return new Options()
        {
            Agvs = numberOfAgvs,
            Sources = new[] { "S1" },
            Destinations = new[] { "D1" },
            GridRows = 3,
            GridGraph = true
        };
    }

    [Ignore]
    [TestMethod]
    public async Task SimulatedAgvProvider_Create_Triggered_CreateAgv_Published()
    {
        var options = GetSimulatedOptions();

        var fixture = new SimulatedAgvProviderTestFixture()
            .WithSimulateOptions(options)
            .Build();

        var services = fixture.Provider.GetRequiredService<IEnumerable<IHostedService>>();
        services.Any(s => s is SimulatedAgvProvider).ShouldBeTrue();
        bool result = await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.CreateAgv>();
        result.ShouldBeTrue();
    }

    [Ignore]
    [TestMethod]
    public async Task TwoAgvs_SingleSourceAndDestination_CreateAgvPublished_AgvIdUnique_Successful()
    {
        var options = GetSimulatedOptions(2);

        var fixture = new SimulatedAgvProviderTestFixture()
            .WithSimulateOptions(options)
            .Build();
        
        var services = fixture.Provider.GetRequiredService<IEnumerable<IHostedService>>();
        services.Any(s => s is SimulatedAgvProvider).ShouldBeTrue();

        (await fixture.SagaHarness.Consumed.Any<SimulatedAgvStateMachine.CreateAgv>()).ShouldBeTrue();

        for (int i = 0; i < options.Agvs; i++)
        {
            int i1 = i;
            var createdSaga = fixture.SagaHarness.Sagas.Select(c => c.CorrelationId == $"SimAgv-{i1}".ToGuid()).First().Saga;

            createdSaga.CorrelationId.ShouldBe($"SimAgv-{i}".ToGuid());
            createdSaga.SerialNumber.ShouldBe($"SimAgv-{i}");
            createdSaga.AgvStatus.ShouldNotBeNull();
            createdSaga.AgvGraph.ShouldNotBeNull();
            createdSaga.AgvData.ShouldNotBeNull();

            (await fixture.TestHarness.Consumed.Any<SimulatedAgvStateMachine.CreateAgvResponse>(c => c.Context.CorrelationId == $"SimAgv-{i}".ToGuid())).ShouldBeTrue();
        }
    }
}