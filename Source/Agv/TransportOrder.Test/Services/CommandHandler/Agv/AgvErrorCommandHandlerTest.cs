using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Test.Helper;
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

        await fixture.PublishMessageAsync(new UpdateErrorStatus(agv.CorrelationId, new List<AgvError>(), new AgvPosition(true, 1, 1, 1, 0)));

        fixture.AgvDbMock.Verify(x => x.Errors.AddAndUpdateAgvErrorAsync(It.IsAny<List<AgvError>>(), It.IsAny<List<AgvError>>(), It.IsAny<Guid>()), Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateSystemAgvErrors_NewErrorAdded_AgvErrorChangedPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        List<AgvError> errors = AgvErrorGenerator.GenerateAgvErrorStatus(2, agv.CorrelationId).Errors;
        var fixture = new AgvErrorCommandHandlerTestFixture()
            .WithActiveAgvErrors([errors[0]])
            .WithAgv(agv);

        await fixture.PublishMessageAsync(new UpdateSystemAgvErrors(Guid.NewGuid(), [errors[1]], [], new AgvPosition(true, 1, 1, 1, 0)));
        var errorChanged = await fixture.GetFirstPublishedMessageAsync<AgvErrorChanged>();
        errorChanged.Errors.Count.ShouldBe(2);
        var errorReceived = await fixture.GetFirstPublishedMessageAsync<AgvErrorReceived>();
        errorReceived.Error.CorrelationId.ShouldBe(errors[1].CorrelationId);
        fixture.VerifyMetricsCollected(Times.Once());
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_NewError_AgvErrorReceivedPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var errorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(1, agv.CorrelationId, position: new AgvPosition(true, 1, 1, 1, 0));
        var fixture = new AgvErrorCommandHandlerTestFixture()
            .WithAgv(agv);

        await fixture.TestHarness.Bus.Publish(new UpdateErrorStatus(agv.CorrelationId, errorStatus.Errors, errorStatus.AgvPosition));
        await fixture.ShouldHaveConsumedAsync<UpdateErrorStatus>();
        await fixture.ShouldHavePublishedAsync<AgvErrorChanged>();
        await fixture.ShouldHavePublishedAsync<AgvErrorReceived>();
        fixture.VerifyMetricsCollected(Times.Once());
    }
}