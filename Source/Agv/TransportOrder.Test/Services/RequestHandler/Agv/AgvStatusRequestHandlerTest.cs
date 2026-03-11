using Gebhardt.Acc.Common.Messages.System;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using MassTransit;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Agv;

[TestClass]
public class AgvStatusRequestHandlerTest
{
    [DataRow(true)]
    [DataRow(false)]
    [TestMethod]
    public async Task Consume_RequestAgvAvailabilityStatus_ValidData_RespondsAgvAvailabilityStatusReceived(bool hasOnlineAgvs)
    {
        AgvRequestsHandlerTestFixture<RequestAgvAvailabilityStatus, AgvStatusRequestHandler> fixture = new AgvRequestsHandlerTestFixture<RequestAgvAvailabilityStatus, AgvStatusRequestHandler>()
            .WithHasOnlineAgvs(hasOnlineAgvs)
            .Build();

        Response<AgvAvailabilityStatusReceived> response = await fixture.GetResponseAsync<AgvAvailabilityStatusReceived>(new RequestAgvAvailabilityStatus());

        response.Message.HasAvailableAgvs.ShouldBe(hasOnlineAgvs);
    }
}