using System.Collections.ObjectModel;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using ResetAgvErrors = Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Commands.ResetAgvErrors;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

[TestClass]
public class ResetAgvErrorsRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_ResetAgvErrorsResult_WithoutSpecifiedErrorIdsToResetButThereAreNoActiveErrors_DoesNotSendResetAgvErrors()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new ResetAgvErrorsRequestHandlerTestFixture()
            .WithActiveAgvErrors(new List<AgvError>())
            .Build();

        await fixture.GetResponseAsync<ResetAgvErrorsResult>(new ResetAgvErrors(agvCorrelationId, new Collection<Guid>()));

        (await fixture.TestHarness.Published.Any<Common.Messages.AgvManagement.Commands.ResetAgvErrors>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ResetAgvErrorsResult_WithoutSpecifiedErrorIdsToResetAndThereAreActiveErrors_ResetsAll()
    {
        var agvCorrelationId = Guid.NewGuid();
        var errors = new List<AgvError> {AgvErrorGenerator.GenerateAgvError()};
        var fixture = new ResetAgvErrorsRequestHandlerTestFixture()
            .WithActiveAgvErrors(errors)
            .Build();

        await fixture.GetResponseAsync<ResetAgvErrorsResult>(new ResetAgvErrors(agvCorrelationId, new Collection<Guid>()));

        var message = await fixture.GetFirstPublishedMessageAsync<Common.Messages.AgvManagement.Commands.ResetAgvErrors>();
        message.CorrelationId.ShouldBe(agvCorrelationId);
        message.ErrorIds.ShouldBeEmpty();
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
        await fixture.GetResponseAsync<ResetAgvErrorsResult>(new ResetAgvErrors(agvCorrelationId, new Collection<Guid> {errorToReset}));

        var message = await fixture.GetFirstPublishedMessageAsync<Common.Messages.AgvManagement.Commands.ResetAgvErrors>();
        message.CorrelationId.ShouldBe(agvCorrelationId);
        message.ErrorIds.Count.ShouldBe(1);
        message.ErrorIds.ShouldContain(errorToReset);
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
        await fixture.GetResponseAsync<ResetAgvErrorsResult>(new ResetAgvErrors(agvCorrelationId, new Collection<Guid> {errorToReset}));

        (await fixture.TestHarness.Published.Any<Common.Messages.AgvManagement.Commands.ResetAgvErrors>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task? Consume_ResetAgvErrorsResult_WithWaitForMessageThrowsException_ThrowsException()
    {
        var agvCorrelationId = Guid.NewGuid();
        var errors = new List<AgvError> {AgvErrorGenerator.GenerateAgvError()};
        var fixture = new ResetAgvErrorsRequestHandlerTestFixture()
            .WithActiveAgvErrors(errors)
            .WithWaitForMessageAction(() => throw new WaitForMessageFailedException<AgvErrorChanged>("Error did not changed"))
            .Build();

        await MediatorShould.ThrowRequestExceptionAsync<ResetAgvErrorFailedException>(async () => await fixture.GetResponseAsync<ResetAgvErrorsResult>(new ResetAgvErrors(agvCorrelationId, new Collection<Guid>())));
    }
}