using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
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

        await fixture.PublishMessageAsync(new ClearAgvCurrentOrder(agv.CorrelationId));

        agv.CurrentOrder.ShouldBeNull();
        agv.CurrentFleetManagerActionId.ShouldBeNull();
        fixture.AgvRepository.Verify(x => x.AddOrUpdateAsync(It.IsAny<Agv>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_ClearAgvCurrentOrder_UnknownAgv_DoesNotCallAddOrUpdate()
    {
        var agvId = Guid.NewGuid();
        var fixture = new AgvCommandHandlerTestFixture()
            .SetNoAgv(agvId);

        await fixture.PublishMessageAsync(new ClearAgvCurrentOrder(agvId));
        await fixture.ShouldHaveConsumedAsync<ClearAgvCurrentOrder>();

        fixture.AgvRepository.Verify(x => x.AddOrUpdateAsync(It.IsAny<Agv>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_AddAgv_NewAgv_MarksPublishedToFleetManagerAndPublishesNewAgvDiscovered()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var agvTypeId = Guid.NewGuid();
        var fixture = new AgvCommandHandlerTestFixture()
            .SetNoAgv(agv.CorrelationId);

        await fixture.PublishMessageAsync(new AddAgv(agv.CorrelationId, agv, agvTypeId));

        fixture.AgvRepository.Verify(x => x.AddOrUpdateAsync(
            It.Is<Agv>(a => a.CorrelationId == agv.CorrelationId && a.PublishedToFleetManager),
            It.IsAny<CancellationToken>()), Times.Once);

        var discovered = await fixture.GetFirstPublishedMessageAsync<NewAgvDiscovered>();
        discovered.CorrelationId.ShouldBe(agv.CorrelationId);
        discovered.SerialNumber.ShouldBe(agv.SerialNumber);
        discovered.AgvTypeId.ShouldBe(agvTypeId);
        discovered.HasValidConfiguration.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AddAgv_ExistingAgvNotYetInFleetManager_MarksPublishedToFleetManagerAndPublishesNewAgvDiscovered()
    {
        var existingAgv = AgvGenerator.Generate(Guid.NewGuid());
        existingAgv.PublishedToFleetManager = false;
        var agv = AgvGenerator.Generate(existingAgv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgv(existingAgv);

        await fixture.PublishMessageAsync(new AddAgv(agv.CorrelationId, agv, null, false));

        fixture.AgvRepository.Verify(x => x.AddOrUpdateAsync(
            It.Is<Agv>(a => a.CorrelationId == agv.CorrelationId && a.PublishedToFleetManager),
            It.IsAny<CancellationToken>()), Times.Once);
        await fixture.ShouldHavePublishedAsync<NewAgvDiscovered>();
    }

    [TestMethod]
    public async Task Consume_AddAgv_ExistingAgvAlreadyInFleetManager_DoesNotAddOrPublish()
    {
        var existingAgv = AgvGenerator.Generate(Guid.NewGuid());
        existingAgv.PublishedToFleetManager = true;
        var agv = AgvGenerator.Generate(existingAgv.CorrelationId);
        var fixture = new AgvCommandHandlerTestFixture()
            .AddAgv(existingAgv);

        await fixture.PublishMessageAsync(new AddAgv(agv.CorrelationId, agv));

        fixture.AgvRepository.Verify(x => x.AddOrUpdateAsync(It.IsAny<Agv>(), It.IsAny<CancellationToken>()), Times.Never);
        await fixture.ShouldNotHavePublishedAsync<NewAgvDiscovered>();
    }

    [TestMethod]
    public async Task Consume_UpdateAgvType_UpdatesAgvAndPublishesAgvVehicleTypeChanged()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var newTypeId = Guid.NewGuid();
        var fixture = new AgvCommandHandlerTestFixture();

        await fixture.PublishMessageAsync(new UpdateAgvType(agv.CorrelationId, newTypeId, agv, false, true));

        fixture.AgvRepository.Verify(x => x.AddOrUpdateAsync(
            It.Is<Agv>(a => a.CorrelationId == agv.CorrelationId),
            It.IsAny<CancellationToken>()), Times.Once);

        var typeChanged = await fixture.GetFirstPublishedMessageAsync<AgvVehicleTypeChanged>();
        typeChanged.CorrelationId.ShouldBe(agv.CorrelationId);
        typeChanged.SerialNumber.ShouldBe(agv.SerialNumber);
        typeChanged.NewTypeId.ShouldBe(newTypeId);
        typeChanged.NewVehicleType.ShouldBe(agv.VehicleTypeId);
        typeChanged.HasValidConfiguration.ShouldBeFalse();
        typeChanged.NeedsConfirmation.ShouldBeTrue();
    }
}