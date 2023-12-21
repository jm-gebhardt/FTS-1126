using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Agv;

[TestClass]
public class AgvErrorCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_UpdateErrorStatus_ValidData_ErrorStatusUpdate()
    {
        var fixture = new AgvErrorCommandHandlerTestFixture();

        await fixture.TestHarness.Bus.Publish(new UpdateErrorStatus(Guid.NewGuid(), new List<AgvError>()));

        (await fixture.ConsumerHarness.Consumed.Any<UpdateErrorStatus>()).ShouldBeTrue();
        fixture.AgvErrorRepository.Verify(x => x.AddOrUpdateAgvErrorStatusAsync(It.IsAny<List<AgvError>>(), It.IsAny<Guid>()), Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_ErrorsChanged_AgvErrorChangedPublished()
    {
        var fixture = new AgvErrorCommandHandlerTestFixture()
            .WithAddOrUpdateAgvErrorStatusReturnValue(true);
        
        await fixture.TestHarness.Bus.Publish(new UpdateErrorStatus(Guid.NewGuid(), new List<AgvError>()));
        (await fixture.ConsumerHarness.Consumed.Any<UpdateErrorStatus>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AgvErrorChanged>()).ShouldBeTrue();
    }
}