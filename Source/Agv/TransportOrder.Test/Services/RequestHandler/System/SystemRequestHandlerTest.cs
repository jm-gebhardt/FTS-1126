using Gebhardt.Acc.TransportOrder.Messages.Requests.System;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.System;

[TestClass]
public class SystemRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_GetSystemTime_RespondsGotSystemTime()
    {
        var now = DateTimeOffset.Now;
        var fixture = new SystemRequestHandlerTestFixture<GetSystemTime>()
            .WithCurrentTime(now)
            .Build();
        fixture.TimeProvider.SetNow(now);

        var response = await fixture.GetResponseAsync<GotSystemTime>(new GetSystemTime(Guid.NewGuid()));
        response.Message.SystemTime.ShouldBe(now.UtcDateTime);
    }
}