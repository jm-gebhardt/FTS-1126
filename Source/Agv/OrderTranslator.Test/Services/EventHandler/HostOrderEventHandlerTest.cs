using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.OrderTranslator.Messages;
using Gebhardt.Acc.OrderTranslator.Test.Helper;

namespace Gebhardt.Acc.OrderTranslator.Test.Services.EventHandler;

[TestClass]
public class HostOrderEventHandlerTest
{
    [TestMethod]
    public async Task Consume_HostOrderCreated_ValidData_PublishesCreateActionGroup()
    {
        var fixture = new HostOrderEventHandlerTestFixture();

        var orderDetails = OrderGenerator.GenerateOrderDetails(OrderGenerator.Ps01, OrderGenerator.Apl1, "Hu");
        await fixture.PublishMessageAsync(new HostOrderCreated("Hu", orderDetails));

        await fixture.ShouldHavePublishedAsync<CreateActionGroup>();
    }
}