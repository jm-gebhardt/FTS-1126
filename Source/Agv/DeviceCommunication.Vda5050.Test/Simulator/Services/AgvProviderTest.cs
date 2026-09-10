using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Shouldly;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services.SimulatedAgvStateMachineTestFixture;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

[TestClass]
public class AgvProviderTest
{
    [TestMethod]
    public async Task AgvProvider_Execute_CreateAgv_Published()
    {
        var fixture = new AgvProviderTestFixture()
            .WithConfiguration(new SimulatedAgvTypeConfiguration(Fixture.AgvTypeIdentifier, 1))
            .Build();

        await fixture.RunBackgroundService();

        (await fixture.SagaHarness.Consumed.Any<CreateAgv>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task TwoAgvs_Execute_CreateAgvPublished_AgvIdUnique_Successful()
    {
        var configuration = new SimulatedAgvTypeConfiguration(Fixture.AgvTypeIdentifier, 2);
        var fixture = new AgvProviderTestFixture()
            .WithConfiguration(configuration)
            .Build();

        await fixture.RunBackgroundService();

        (await fixture.SagaHarness.Consumed.Any<CreateAgv>()).ShouldBeTrue();

        foreach (string serialNumber in configuration.SerialNumbers())
        {
            var correlationId = serialNumber.ToGuid();

            var createdSaga = fixture.SagaHarness.Sagas.Select(s => s.CorrelationId == correlationId).First().Saga;

            createdSaga.CorrelationId.ShouldBe(correlationId);
            createdSaga.Configuration.SerialNumber.ShouldBe(serialNumber);
        }
    }
}