using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Commands;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class ResetAgvErrorsRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_ResetAgvErrorsResult_WithoutSpecifiedErrorIdsToResetButThereAreNoActiveErrors_DoesNotSendResetAgvErrors()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new ResetAgvErrorsRequestHandlerTestFixture()
            .WithActiveAgvErrors([])
            .WithAgv(agv)
            .Build();

        await fixture.GetResponseAsync<ResetAgvErrorsResult>(new ResetAgvErrors(agv.CorrelationId, [], "user"));

        (await fixture.TestHarness.Published.Any<Common.Messages.AgvManagement.Commands.ResetAgvErrors>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ResetAgvErrorsResult_WithoutSpecifiedErrorIdsToResetAndThereAreActiveErrors_ResetsAllAgvErrors()
    {
        var agvCorrelationId = Guid.NewGuid();
        var errors = new List<AgvError> { AgvErrorGenerator.GenerateAgvError(errorSource: AgvErrorSource.Agv) };
        var fixture = new ResetAgvErrorsRequestHandlerTestFixture()
            .WithActiveAgvErrors(errors)
            .Build();

        await fixture.GetResponseAsync<ResetAgvErrorsResult>(new ResetAgvErrors(agvCorrelationId, [], "user"));

        var message = await fixture.GetFirstPublishedMessageAsync<Common.Messages.AgvManagement.Commands.ResetAgvErrors>();
        message.CorrelationId.ShouldBe(agvCorrelationId);
        message.ErrorIds.Count.ShouldBe(errors.Count);
        message.ErrorIds.Single().ShouldBe(errors[0].CorrelationId);
    }

    [TestMethod]
    public async Task Consume_ResetAgvErrorsResult_WithSpecifiedErrorIdsToResetAndErrorIsActive_ResetsThisSpecificError()
    {
        var agvCorrelationId = Guid.NewGuid();
        var errors = new List<AgvError>
        {
            AgvErrorGenerator.GenerateAgvError(),
            AgvErrorGenerator.GenerateAgvError(),
            AgvErrorGenerator.GenerateAgvError()
        };

        var fixture = new ResetAgvErrorsRequestHandlerTestFixture()
            .WithActiveAgvErrors(errors)
            .Build();

        var errorToReset = errors[0].CorrelationId;
        await fixture.GetResponseAsync<ResetAgvErrorsResult>(new ResetAgvErrors(agvCorrelationId, [errorToReset], "user"));

        var message = await fixture.GetFirstPublishedMessageAsync<Common.Messages.AgvManagement.Commands.ResetAgvErrors>();
        message.CorrelationId.ShouldBe(agvCorrelationId);
        message.ErrorIds.Count.ShouldBe(1);
        message.ErrorIds.ShouldContain(errorToReset);
    }

    [TestMethod]
    public async Task Consume_ResetAgvErrorsResult_WithSystemAndAgvErrors_SendsCommandsForBoth()
    {
        var agvCorrelationId = Guid.NewGuid();
        var errors = new List<AgvError>
        {
            AgvErrorGenerator.GenerateAgvError(),
            AgvErrorGenerator.GenerateAgvError(errorSource: AgvErrorSource.System)
        };

        var fixture = new ResetAgvErrorsRequestHandlerTestFixture()
            .WithActiveAgvErrors(errors)
            .Build();

        await fixture.GetResponseAsync<ResetAgvErrorsResult>(new ResetAgvErrors(agvCorrelationId, [errors[0].CorrelationId, errors[1].CorrelationId], "user"));

        var resetAgvErrors = await fixture.GetFirstPublishedMessageAsync<Common.Messages.AgvManagement.Commands.ResetAgvErrors>();
        resetAgvErrors.CorrelationId.ShouldBe(agvCorrelationId);
        resetAgvErrors.ErrorIds.Count.ShouldBe(1);
        resetAgvErrors.ErrorIds.Single().ShouldBe(errors[0].CorrelationId);

        var resolveSystemAgvError = await fixture.GetFirstPublishedMessageAsync<ResolveSystemAgvError>();
        resolveSystemAgvError.CorrelationId.ShouldBe(agvCorrelationId);
        resolveSystemAgvError.Errors.Count.ShouldBe(1);
        resolveSystemAgvError.Errors.Single().CorrelationId.ShouldBe(errors[1].CorrelationId);
    }

    [TestMethod]
    public async Task Consume_ResetAgvErrorsResult_WithSpecifiedErrorIdsToResetButErrorIsNotActive_DoesNotSendResetAgvErrors()
    {
        var agvCorrelationId = Guid.NewGuid();
        var errors = new List<AgvError>
        {
            AgvErrorGenerator.GenerateAgvError(),
            AgvErrorGenerator.GenerateAgvError(),
            AgvErrorGenerator.GenerateAgvError()
        };
        var fixture = new ResetAgvErrorsRequestHandlerTestFixture()
            .WithActiveAgvErrors(errors)
            .Build();

        var errorToReset = Guid.NewGuid();
        await fixture.GetResponseAsync<ResetAgvErrorsResult>(new ResetAgvErrors(agvCorrelationId, [errorToReset], "user"));

        (await fixture.TestHarness.Published.Any<Common.Messages.AgvManagement.Commands.ResetAgvErrors>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task? Consume_ResetAgvErrorsResult_WithWaitForMessageThrowsException_ThrowsException()
    {
        var agvCorrelationId = Guid.NewGuid();
        var errors = new List<AgvError> { AgvErrorGenerator.GenerateAgvError() };
        var fixture = new ResetAgvErrorsRequestHandlerTestFixture()
            .WithActiveAgvErrors(errors)
            .WithWaitForMessageAction(() => throw new WaitForMessageFailedException<AgvErrorChanged>("Error did not changed"))
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<ResetAgvErrorFailedException>(async () => await fixture.GetResponseAsync<ResetAgvErrorsResult>(new ResetAgvErrors(agvCorrelationId, [], "user")));
    }

    [TestMethod]
    public async Task Consume_ResetAgvErrorsResult_WhenAgvDoesNotExist_ThrowsException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new ResetAgvErrorsRequestHandlerTestFixture()
            .WithActiveAgvErrors([])
            .WithAgv(agv)
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<ResetAgvErrorsResult>(new ResetAgvErrors(Guid.NewGuid(), [], "user")));
    }
}