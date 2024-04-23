using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.System;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.RequestHandler.System;

[TestClass]
public class SystemRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetSystemTime_RespondsGotSystemTime()
    {
        var now = DateTime.Now;
        SystemRequestHandlerTestFixture<GetSystemTime> fixture = new SystemRequestHandlerTestFixture<GetSystemTime>()
            .WithCurrentTime(now)
            .Build();
        fixture.TimeProvider.SetNow(now);

        Response<GotSystemTime> response = await fixture.GetResponseAsync<GotSystemTime>(new GetSystemTime(Guid.NewGuid()));
        response.Message.SystemTime.ShouldBe(now);
    }
}