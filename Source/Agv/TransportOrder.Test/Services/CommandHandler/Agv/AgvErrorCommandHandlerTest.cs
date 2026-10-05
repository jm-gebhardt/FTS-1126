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
            .WithAgv(agv)
            .WithActionGroups([]);

        await fixture.PublishMessageAsync(new UpdateErrorStatus(agv.CorrelationId, new List<AgvError>(), new AgvPosition(true, 1, 1, 1, 0)));

        fixture.AgvDbMock.Verify(x => x.Errors.AddAndUpdateAgvErrorAsync(It.IsAny<List<AgvError>>(), It.IsAny<List<AgvError>>(), It.IsAny<Guid>()), Times.Never());
    }

    [TestMethod]
    public async Task Consume_UpdateSystemAgvErrors_NewErrorAdded_AgvErrorChangedPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var errors = AgvErrorGenerator.GenerateAgvErrorStatus(2, agv.CorrelationId).Errors;
        var fixture = new AgvErrorCommandHandlerTestFixture()
            .WithActiveAgvErrors([errors[0]])
            .WithAgv(agv)
            .WithActionGroups([]);

        await fixture.PublishMessageAsync(new UpdateSystemAgvErrors(Guid.NewGuid(), [errors[1]], [], new AgvPosition(true, 1, 1, 1, 0)));
        var errorChanged = await fixture.GetFirstPublishedMessageAsync<AgvErrorChanged>();
        errorChanged.Errors.Count.ShouldBe(2);
        var errorReceived = await fixture.GetFirstPublishedMessageAsync<AgvErrorReceived>();
        errorReceived.Error.CorrelationId.ShouldBe(errors[1].CorrelationId);
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_NewError_AgvErrorReceivedPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var errorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(1, agv.CorrelationId, position: new AgvPosition(true, 1, 1, 1, 0));
        var fixture = new AgvErrorCommandHandlerTestFixture()
            .WithAgv(agv)
            .WithActionGroups([]);

        await fixture.TestHarness.Bus.Publish(new UpdateErrorStatus(agv.CorrelationId, errorStatus.Errors, errorStatus.AgvPosition));
        await fixture.ShouldHaveConsumedAsync<UpdateErrorStatus>();
        await fixture.ShouldHavePublishedAsync<AgvErrorChanged>();
        await fixture.ShouldHavePublishedAsync<AgvErrorReceived>();
    }

    [TestMethod]
    public async Task Consume_UpdateErrorStatus_WifiMessage_NotStoredInDb_AgvErrorReceivedNotPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var errorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(2, agv.CorrelationId, errorType: "WIFI", errorLevel: ErrorLevel.Information);
        var fixture = new AgvErrorCommandHandlerTestFixture()
            .WithActiveAgvErrors([])
            .WithAgv(agv)
            .WithActionGroups([]);

        var errorChanged = await fixture.PublishAndGetFirstMessageAsync<UpdateErrorStatus, AgvErrorChanged>(new UpdateErrorStatus(agv.CorrelationId, [errorStatus.Errors.Last()], errorStatus.AgvPosition));
        errorChanged.NewErrors.Count.ShouldBe(1);
        errorChanged.ResolvedErrors.Count.ShouldBe(0);
        errorChanged.Errors.ShouldContain(e => e.CorrelationId == errorStatus.Errors.Last().CorrelationId);
        fixture.AgvDbMock.Verify(x => x.Errors.AddAndUpdateAgvErrorAsync(It.IsAny<List<AgvError>>(), It.IsAny<List<AgvError>>(), It.IsAny<Guid>()), Times.Never);
        await fixture.ShouldNotHavePublishedAsync<AgvErrorReceived>();
    }

    [TestMethod]
    public async Task Consume_UpdateSystemAgvErrors_OnlyWifiMessageWithResolvedError_AddAndUpdateAgvErrorCalled()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var existingErrors = AgvErrorGenerator.GenerateAgvErrorStatus(1, agv.CorrelationId).Errors;
        var wifiErrorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(1, agv.CorrelationId, errorType: "WIFI", errorLevel: ErrorLevel.Information);
        var fixture = new AgvErrorCommandHandlerTestFixture()
            .WithActiveAgvErrors(existingErrors)
            .WithAgv(agv)
            .WithActionGroups([]);

        await fixture.PublishMessageAsync(new UpdateSystemAgvErrors(Guid.NewGuid(), wifiErrorStatus.Errors, [existingErrors[0]], new AgvPosition(true, 1, 1, 1, 0)));

        fixture.AgvDbMock.Verify(x => x.Errors.AddAndUpdateAgvErrorAsync(It.Is<List<AgvError>>(l => l.Count == 0), It.Is<List<AgvError>>(l => l.Count == 1), It.IsAny<Guid>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_UpdateSystemAgvErrors_JoyStickWarningResolved_TryRemoveQueueBlockingPublished()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var errors = AgvErrorGenerator.GenerateAgvErrorStatus(1, agv.CorrelationId, description: AgvError.JoystickMode).Errors;
        var fixture = new AgvErrorCommandHandlerTestFixture()
            .WithActiveAgvErrors([errors[0]])
            .WithAgv(agv)
            .WithActionGroups([]);

        await fixture.PublishMessageAsync(new UpdateSystemAgvErrors(Guid.NewGuid(), [],[errors[0]], new AgvPosition(true, 1, 1, 1, 0)));
        var errorChanged = await fixture.GetFirstPublishedMessageAsync<AgvErrorChanged>();
        errorChanged.Errors.Count.ShouldBe(1);

        await fixture.ShouldHavePublishedAsync<TryRemoveQueueBlocking>();
    }
}