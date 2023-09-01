using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.Agv;

[TestClass]
public class GetAgvErrorsRequestHandlerTest
{
    private static void VerifyAgvErrorsMessageContent(IEnumerable<AgvError>? agvErrorsDto, AgvErrorStatusReceived agvErrorStatus)
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
    public async Task GetAgvErrorsRequestHandler_WhenThereAreNoActiveErrorsForAgv_ReturnsEmptyAgvErrors()
    {
        var agvCorrelationId = Guid.NewGuid();            
        AgvErrorRequestHandlerTestFixture<GetActiveAgvErrorsRequest, GetAgvErrorsRequestHandler> fixture = await new AgvErrorRequestHandlerTestFixture<GetActiveAgvErrorsRequest, GetAgvErrorsRequestHandler>()
            .BuildAsync();

        Response<GetActiveAgvErrorsResult> response = await fixture.GetResponseAsync<GetActiveAgvErrorsResult>(new GetActiveAgvErrorsRequest(agvCorrelationId));

        List<AgvError> agvErrors = response.Message.AgvErrors;
        agvErrors.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvErrorsRequestHandler_WhenThereAreActiveErrorsForAgv_ReturnsAgvErrors()
    {
        var agvCorrelationId = Guid.NewGuid();
        const int errorCount = 5;
        var agvErrorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(errorCount, agvCorrelationId: agvCorrelationId);
        AgvErrorRequestHandlerTestFixture<GetActiveAgvErrorsRequest, GetAgvErrorsRequestHandler> fixture = await new AgvErrorRequestHandlerTestFixture<GetActiveAgvErrorsRequest, GetAgvErrorsRequestHandler>()
            .WithAgvErrorStatus(agvErrorStatus)
            .BuildAsync();

        Response<GetActiveAgvErrorsResult> response = await fixture.GetResponseAsync<GetActiveAgvErrorsResult>(new GetActiveAgvErrorsRequest(agvCorrelationId));

        List<AgvError> agvErrors = response.Message.AgvErrors;
        agvErrors.Count.ShouldBe(errorCount);
        VerifyAgvErrorsMessageContent(agvErrors, agvErrorStatus);
    }
}