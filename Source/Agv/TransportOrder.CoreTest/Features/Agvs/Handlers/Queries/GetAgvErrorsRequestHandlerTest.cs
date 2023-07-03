using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Features.Agvs.Handlers.Queries;

[TestClass]
public class GetAgvErrorsRequestHandlerTest
{
    private static void VerifyAgvErrorsDto(IEnumerable<AgvErrorDto>? agvErrorsDto, AgvErrorStatusReceived agvErrorStatus)
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
        AgvErrorRequestHandlerTestFixture<GetActiveAgvErrorsRequest, GetActiveAgvErrorsRequestHandler> fixture = await new AgvErrorRequestHandlerTestFixture<GetActiveAgvErrorsRequest, GetActiveAgvErrorsRequestHandler>()
            .BuildAsync();

        Response<GetActiveAgvErrorsResult> response = await fixture.GetResponseAsync<GetActiveAgvErrorsResult>(new GetActiveAgvErrorsRequest(agvCorrelationId));

        List<AgvErrorDto> agvErrors = response.Message.AgvErrorsDto;
        agvErrors.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task GetAgvErrorsRequestHandler_WhenThereAreActiveErrorsForAgv_ReturnsAgvErrors()
    {
        var agvCorrelationId = Guid.NewGuid();
        const int errorCount = 5;
        var agvErrorStatus = AgvErrorGenerator.GenerateAgvErrorStatus(errorCount, agvCorrelationId: agvCorrelationId);
        AgvErrorRequestHandlerTestFixture<GetActiveAgvErrorsRequest, GetActiveAgvErrorsRequestHandler> fixture = await new AgvErrorRequestHandlerTestFixture<GetActiveAgvErrorsRequest, GetActiveAgvErrorsRequestHandler>()
            .WithAgvErrorStatus(agvErrorStatus)
            .BuildAsync();

        Response<GetActiveAgvErrorsResult> response = await fixture.GetResponseAsync<GetActiveAgvErrorsResult>(new GetActiveAgvErrorsRequest(agvCorrelationId));

        List<AgvErrorDto> agvErrors = response.Message.AgvErrorsDto;
        agvErrors.Count.ShouldBe(errorCount);
        VerifyAgvErrorsDto(agvErrors, agvErrorStatus);
    }
}