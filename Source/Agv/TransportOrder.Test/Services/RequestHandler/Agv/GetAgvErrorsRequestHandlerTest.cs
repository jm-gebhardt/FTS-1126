using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class GetAgvErrorsRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetActiveAgvErrors_NoActiveErrorsForAgv_ReturnsEmptyAgvErrors()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<GetActiveAgvErrors, GetAgvErrorsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetActiveAgvErrors, GetAgvErrorsRequestHandler>()
            .WithEmptyAgvErrorRepository()
            .WithAgv(agv)
            .Build();
        Response<GotActiveAgvErrors> response = await fixture.GetResponseAsync<GotActiveAgvErrors>(new GetActiveAgvErrors(agv.CorrelationId));

        List<AgvError> agvErrors = response.Message.AgvErrors;
        agvErrors.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_GetActiveAgvErrors_ValidData_ReturnsAgvErrors()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var agvError = AgvErrorGenerator.GenerateAgvError(agv.CorrelationId);
        AgvRequestsHandlerTestFixture<GetActiveAgvErrors, GetAgvErrorsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetActiveAgvErrors, GetAgvErrorsRequestHandler>()
            .WithActiveAgvErrors(agv.CorrelationId, new List<Common.Domain.Models.AgvError> { agvError })
            .WithAgv(agv)
            .Build();

        Response<GotActiveAgvErrors> response = await fixture.GetResponseAsync<GotActiveAgvErrors>(new GetActiveAgvErrors(agv.CorrelationId));

        List<AgvError> actualAgvErrors = response.Message.AgvErrors;
        actualAgvErrors.Count.ShouldBe(1);
        actualAgvErrors.Single().AgvCorrelationId.ShouldBe(agv.CorrelationId);
        actualAgvErrors.Single().ErrorType.ShouldBe(agvError.ErrorType);
    }

    [TestMethod]
    public async Task Consume_GetActiveAgvErrors_UnknownCorrelationId_ThrowsException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var agvError = AgvErrorGenerator.GenerateAgvError(agv.CorrelationId);
        AgvRequestsHandlerTestFixture<GetActiveAgvErrors, GetAgvErrorsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetActiveAgvErrors, GetAgvErrorsRequestHandler>()
            .WithActiveAgvErrors(agv.CorrelationId, new List<Common.Domain.Models.AgvError> { agvError })
            .WithAgv(agv)
            .Build();
        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GotActiveAgvErrors>(new GetActiveAgvErrors("INVALID".ToGuid())));
    }

    [TestMethod]
    public async Task Consume_GetAgvErrors_EmptyAgvErrors_ReturnsEmptyAgvErrorHistory()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        AgvRequestsHandlerTestFixture<GetAgvErrors, GetAgvErrorsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvErrors, GetAgvErrorsRequestHandler>()
            .WithEmptyAgvErrorRepository()
            .WithAgv(agv)
            .Build();

        Response<GotAgvErrors> response = await fixture.GetResponseAsync<GotAgvErrors>(new GetAgvErrors(agv.CorrelationId, DateTime.Today.AddDays(-1), DateTime.UtcNow));

        List<AgvError> agvErrors = response.Message.AgvErrors;
        agvErrors.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_GetAgvErrors_WithTimeFilter_ReturnsAgvErrorHistory()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        List<Common.Domain.Models.AgvError> errors = Enumerable.Range(0, 2).Select(_ => AgvErrorGenerator.GenerateAgvError(agv.CorrelationId)).ToList();
        AgvRequestsHandlerTestFixture<GetAgvErrors, GetAgvErrorsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvErrors, GetAgvErrorsRequestHandler>()
            .WithActiveAgvErrors(agv.CorrelationId, errors)
            .WithAgv(agv)
            .Build();

        Response<GotAgvErrors> response = await fixture.GetResponseAsync<GotAgvErrors>(new GetAgvErrors(agv.CorrelationId, DateTime.Today.AddDays(-1), DateTime.UtcNow));

        List<AgvError> agvErrors = response.Message.AgvErrors;
        agvErrors.Count.ShouldBe(errors.Count);
    }

    [TestMethod]
    public async Task Consume_GetAgvErrors_UnknownCorrelationId_ThrowsException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var agvError = AgvErrorGenerator.GenerateAgvError(agv.CorrelationId);
        AgvRequestsHandlerTestFixture<GetAgvErrors, GetAgvErrorsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetAgvErrors, GetAgvErrorsRequestHandler>()
            .WithActiveAgvErrors(agv.CorrelationId, new List<Common.Domain.Models.AgvError> { agvError })
            .WithAgv(agv)
            .Build();
        await MediatorShould.ThrowRequestExceptionAsync<AgvNotFoundException>(async () => await fixture.GetResponseAsync<GotAgvErrors>(new GetAgvErrors("INVALID".ToGuid(), DateTime.Today.AddDays(-1), DateTime.UtcNow)));
    }

    [TestMethod]
    public async Task Consume_GetActiveErrorsForAgvs_ValidData_ReturnsErrorsForAgvs()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var agvs = new List<TransportOrder.Domain.Models.Agv.Agv> { agv };
        var agvError = AgvErrorGenerator.GenerateAgvError(agv.CorrelationId);
        var errors = new List<Common.Domain.Models.AgvError> { agvError };
        AgvRequestsHandlerTestFixture<GetActiveErrorsForAgvs, GetAgvErrorsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetActiveErrorsForAgvs, GetAgvErrorsRequestHandler>()
            .WithActiveAgvErrors(agv.CorrelationId, errors)
            .WithAgvs(agvs)
            .Build();

        Response<GotActiveErrorsForAgvs> response = await fixture.GetResponseAsync<GotActiveErrorsForAgvs>(new GetActiveErrorsForAgvs(new List<Guid> { agv.CorrelationId }));

        List<AgvErrors> actualErrorsForAgvs = response.Message.AgvsErrors;

        actualErrorsForAgvs.Count.ShouldBe(agvs.Count);
        List<AgvError> actualErrorsForAgv = actualErrorsForAgvs.FindAll(e => e.CorrelationId == agv.CorrelationId).Select(e => e.AgvErrorList).First().ToList();
        actualErrorsForAgv.Count.ShouldBe(errors.Count);
        actualErrorsForAgv.Single().CorrelationId.ShouldBe(agvError.CorrelationId);
        actualErrorsForAgv.Single().ErrorType.ShouldBe(agvError.ErrorType);
    }

    [TestMethod]
    public async Task Consume_GetActiveErrorsForAgvs_AgvsNotFound_ThrowsAgvsNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var agvError = AgvErrorGenerator.GenerateAgvError(agv.CorrelationId);
        AgvRequestsHandlerTestFixture<GetActiveErrorsForAgvs, GetAgvErrorsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetActiveErrorsForAgvs, GetAgvErrorsRequestHandler>()
            .WithActiveAgvErrors(agv.CorrelationId, new List<Common.Domain.Models.AgvError> { agvError })
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .Build();
        await MediatorShould.ThrowRequestExceptionAsync<AgvsNotFoundException>(async () => await fixture.GetResponseAsync<GotActiveErrorsForAgvs>(new GetActiveErrorsForAgvs(new List<Guid> { "INVALID".ToGuid() })));
    }

    [TestMethod]
    public async Task Consume_GetErrorHistoryForAgvs_ValidData_ReturnsErrorsForAgvs()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var agvs = new List<TransportOrder.Domain.Models.Agv.Agv> { agv };
        var agvError = AgvErrorGenerator.GenerateAgvError(agv.CorrelationId);
        var errors = new List<Common.Domain.Models.AgvError> { agvError };
        AgvRequestsHandlerTestFixture<GetErrorHistoryForAgvs, GetAgvErrorsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetErrorHistoryForAgvs, GetAgvErrorsRequestHandler>()
            .WithActiveAgvErrors(agv.CorrelationId, errors)
            .WithAgvs(agvs)
            .Build();

        Response<GotErrorHistoryForAgvs> response = await fixture.GetResponseAsync<GotErrorHistoryForAgvs>(new GetErrorHistoryForAgvs(new List<Guid> { agv.CorrelationId }, DateTime.Today.AddDays(-1), DateTime.UtcNow));

        List<AgvErrors> actualErrorsForAgvs = response.Message.AgvsErrors;

        actualErrorsForAgvs.Count.ShouldBe(agvs.Count);
        List<AgvError> actualErrorsForAgv = actualErrorsForAgvs.FindAll(e => e.CorrelationId == agv.CorrelationId).Select(e => e.AgvErrorList).First().ToList();
        actualErrorsForAgv.Count.ShouldBe(errors.Count);
        actualErrorsForAgv.Single().CorrelationId.ShouldBe(agvError.CorrelationId);
        actualErrorsForAgv.Single().ErrorType.ShouldBe(agvError.ErrorType);
    }

    [TestMethod]
    public async Task Consume_GetErrorHistoryForAgvs_AgvsNotFound_ThrowsAgvsNotFoundException()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var agvError = AgvErrorGenerator.GenerateAgvError(agv.CorrelationId);
        AgvRequestsHandlerTestFixture<GetErrorHistoryForAgvs, GetAgvErrorsRequestHandler> fixture = new AgvRequestsHandlerTestFixture<GetErrorHistoryForAgvs, GetAgvErrorsRequestHandler>()
            .WithActiveAgvErrors(agv.CorrelationId, new List<Common.Domain.Models.AgvError> { agvError })
            .WithAgvs(new List<TransportOrder.Domain.Models.Agv.Agv> { agv })
            .Build();
        await MediatorShould.ThrowRequestExceptionAsync<AgvsNotFoundException>(async () => await fixture.GetResponseAsync<GotErrorHistoryForAgvs>(new GetErrorHistoryForAgvs(new List<Guid> { "INVALID".ToGuid() }, DateTime.Today.AddDays(-1), DateTime.UtcNow)));
    }
}