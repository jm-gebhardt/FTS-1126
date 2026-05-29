using Gebhardt.Acc.Common.Messages.ActionGroups.Vehicle;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.TrafficManagement;

[TestClass]
public class NewBaseRequestConsumerTest
{
    [TestMethod]
    public async Task Consume_AgvNewBaseRequested_CallsHandleNewBaseRequestAsync()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new NewBaseRequestConsumerTestFixture();

        await fixture.PublishMessageAsync(new AgvNewBaseRequested(agvCorrelationId));

        fixture.TrafficManager.Verify(x => x.HandleNewBaseRequestAsync(agvCorrelationId, It.IsAny<CancellationToken>()), Times.Once);
    }
}