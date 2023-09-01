using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

[TestClass]
public class GetAgvErrorHistoryRequestHandlerTest
{
    private static void VerifyAgvErrorsMessageContent
        (IEnumerable<AgvError>? agvErrorsDto, AgvErrorStatusReceived agvErrorStatus)
    {
        if (agvErrorsDto == null)
        {
            agvErrorStatus.ShouldBeNull();
            return;
        }

        foreach (var actualAgvError in agvErrorsDto)
        {
            actualAgvError.AgvCorrelationId.ShouldBe(agvErrorStatus.CorrelationId);
            var expectedAgvError = agvErrorStatus.Errors!.Find(e => e.ErrorType == actualAgvError.ErrorType);
            expectedAgvError.ShouldNotBeNull();
            actualAgvError.ErrorDescription.ShouldBe(expectedAgvError.ErrorDescription);
            actualAgvError.ErrorLevel.ShouldBe(expectedAgvError.ErrorLevel);
            actualAgvError.ErrorType.ShouldBe(expectedAgvError.ErrorType);
            actualAgvError.ErrorReferences.ShouldBe(expectedAgvError.ErrorReferences);
        }
    }

    [TestMethod]
    public async Task GetAgvErrorHistoryRequestHandler_EmptyAgvErrors_ReturnsEmptyAgvErrorHistory()
    {
        var agvCorrelationId = Guid.NewGuid();
        AgvErrorRequestHandlerTestFixture<GetAgvErrorsRequest, GetAgvErrorsRequestHandler> fixture = await new AgvErrorRequestHandlerTestFixture<GetAgvErrorsRequest, GetAgvErrorsRequestHandler>()
            .BuildAsync();

        Response<GetAgvErrorsResult> response = await fixture.GetResponseAsync<GetAgvErrorsResult>(new GetAgvErrorsRequest(agvCorrelationId, DateTime.Today.AddDays(-1), DateTime.UtcNow));

        List<AgvError> agvErrors = response.Message.AgvErrors;
        agvErrors.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvErrorHistoryRequestHandler_WhenThereAreErrorsWithTimeFilter_ReturnsAgvErrorHistory()
    {
        var agvCorrelationId = Guid.NewGuid();
        const int errorCount = 5;
        var agvErrorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(errorCount, agvCorrelationId: agvCorrelationId);
        AgvErrorRequestHandlerTestFixture<GetAgvErrorsRequest, GetAgvErrorsRequestHandler> fixture = await new AgvErrorRequestHandlerTestFixture<GetAgvErrorsRequest, GetAgvErrorsRequestHandler>()
            .WithAgvErrorStatus(agvErrorStatus)
            .BuildAsync();

        Response<GetAgvErrorsResult> response = await fixture.GetResponseAsync<GetAgvErrorsResult>(new GetAgvErrorsRequest(agvCorrelationId, DateTime.Today.AddDays(-1), DateTime.UtcNow));

        List<AgvError> agvErrors = response.Message.AgvErrors;
        agvErrors.Count.ShouldBe(errorCount);
        VerifyAgvErrorsMessageContent(agvErrors, agvErrorStatus);
    }

    [TestMethod]
    public async Task GetAgvErrorHistoryRequestHandler_WhenThereAreErrors_ReturnsAgvErrorHistory()
    {
        var agvCorrelationId = Guid.NewGuid();
        var agvErrorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(1, agvCorrelationId: agvCorrelationId);
        AgvErrorRequestHandlerTestFixture<GetAgvErrorsRequest, GetAgvErrorsRequestHandler> fixture = await new AgvErrorRequestHandlerTestFixture<GetAgvErrorsRequest, GetAgvErrorsRequestHandler>()
            .WithAgvErrorStatus(agvErrorStatus)
            .BuildAsync();

        var error = (await fixture.GetResponseAsync<GetAgvErrorsResult>(new GetAgvErrorsRequest(agvCorrelationId, DateTime.MinValue, DateTime.MaxValue))).Message.AgvErrors.Single();

        // border case where dateFrom and dateTo equals the actual time of the error 
        Response<GetAgvErrorsResult> response = await fixture.GetResponseAsync<GetAgvErrorsResult>(new GetAgvErrorsRequest(agvCorrelationId, error.ErrorReceived, error.ErrorReceived));

        List<AgvError> agvErrors = response.Message.AgvErrors;
        agvErrors.Count.ShouldBe(1);
        agvErrors.Single().ShouldNotBeNull();
    }
}