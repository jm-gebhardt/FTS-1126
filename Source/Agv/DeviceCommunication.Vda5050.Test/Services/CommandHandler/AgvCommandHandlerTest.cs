using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Commands.Agv;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

[TestClass]
public class AgvCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_ClearAgvCurrentOrder_ExistingAgv_ClearsOrderAndAction()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid(), currentOrder: Guid.NewGuid(), currentFleetManagerActionId: Guid.NewGuid());
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgv(agv);

        await fixture.TestHarness.Bus.Publish(new ClearAgvCurrentOrder(agv.CorrelationId));
        await fixture.ShouldHaveConsumedAsync<ClearAgvCurrentOrder>();

        agv.CurrentOrder.ShouldBeNull();
        agv.CurrentFleetManagerActionId.ShouldBeNull();
        fixture.AgvRepository.Verify(x => x.AddOrUpdateAsync(It.IsAny<Agv>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_ClearAgvCurrentOrder_UnknownAgv_DoesNotCallAddOrUpdate()
    {
        var agvId = Guid.NewGuid();
        var fixture = new AgvCommandHandlerTestFixture()
            .SetNoAgv(agvId);

        await fixture.TestHarness.Bus.Publish(new ClearAgvCurrentOrder(agvId));
        await fixture.ShouldHaveConsumedAsync<ClearAgvCurrentOrder>();

        fixture.AgvRepository.Verify(x => x.AddOrUpdateAsync(It.IsAny<Agv>()), Times.Never);
    }
}