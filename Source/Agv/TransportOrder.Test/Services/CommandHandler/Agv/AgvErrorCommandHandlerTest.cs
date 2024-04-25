using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.Agv;

[TestClass]
public class AgvErrorCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_UpdateErrorStatus_ValidData_ErrorStatusUpdate()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvErrorCommandHandlerTestFixture()
            .WithAgv(agv);

        await fixture.TestHarness.Bus.Publish(new UpdateErrorStatus(agv.CorrelationId, new List<AgvError>(), new AgvPosition(true, 1, 1, 1, 0)));

        (await fixture.ConsumerHarness.Consumed.Any<UpdateErrorStatus>()).ShouldBeTrue();
        fixture.AgvDbMock.Verify(x => x.Errors.AddOrUpdateAgvErrorStatusAsync(It.IsAny<List<AgvError>>(), It.IsAny<Guid>()), Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_ErrorsChanged_AgvErrorChangedPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new AgvErrorCommandHandlerTestFixture()
            .WithAgv(agv)
            .WithAddOrUpdateAgvErrorStatusReturnValue(true, []);

        await fixture.TestHarness.Bus.Publish(new UpdateErrorStatus(Guid.NewGuid(), new List<AgvError>(), new AgvPosition(true, 1, 1, 1, 0)));
        await fixture.ShouldHaveConsumedAsync<UpdateErrorStatus>();
        await fixture.ShouldHavePublishedAsync<AgvErrorChanged>();
        await fixture.ShouldNotHavePublishedAsync<AgvErrorReceived>();
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_NewError_AgvErrorReceivedPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var errorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(1, agv.CorrelationId, position: new AgvPosition(true,1, 1, 1, 0));
        var fixture = new AgvErrorCommandHandlerTestFixture()
            .WithAgv(agv)
            .WithAddOrUpdateAgvErrorStatusReturnValue(true, errorStatus.Errors!);

        await fixture.TestHarness.Bus.Publish(new UpdateErrorStatus(agv.CorrelationId, errorStatus.Errors!, errorStatus.AgvPosition));
        await fixture.ShouldHaveConsumedAsync<UpdateErrorStatus>();
        await fixture.ShouldHavePublishedAsync<AgvErrorChanged>();
        await fixture.ShouldHavePublishedAsync<AgvErrorReceived>();
    }
}