using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.OrderTranslator.Test.Helper;
using Moq;

namespace Gebhardt.Acc.OrderTranslator.Test.Services.CommandHandler;

[TestClass]
public class HostOrderCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_CreateActionGroup_ValidData_PublishesCreateActionGroup()
    {
        var orderDetails = OrderGenerator.GenerateOrderDetails(OrderGenerator.Apl1, OrderGenerator.Ps01, "hu");

        var fixture = new HostOrderCommandHandlerTestFixture()
            .WithValidationError(null)
            .WithStations([OrderGenerator.Apl1], [OrderGenerator.Ps01, OrderGenerator.Ps02]);

        await fixture.PublishMessageAsync(new Messages.CreateActionGroup(orderDetails.LeNo, orderDetails));

        await fixture.ShouldHavePublishedAsync<CreateActionGroup>();
        fixture.VerifyHostOrderAdded(Times.Once());
    }

    [TestMethod]
    public async Task Consume_CreateActionGroup_ValidationError_PublishesHostOrderValidationFailed()
    {
        var orderDetails = OrderGenerator.GenerateOrderDetails(OrderGenerator.Apl1, OrderGenerator.Ps01, "hu");

        var fixture = new HostOrderCommandHandlerTestFixture()
            .WithValidationError(ValidationError.SourceUnknown)
            .WithStations([OrderGenerator.Apl1], [OrderGenerator.Ps01, OrderGenerator.Ps02]);

        await fixture.PublishMessageAsync(new Messages.CreateActionGroup(orderDetails.LeNo, orderDetails));

        await fixture.ShouldHavePublishedAsync<HostOrderValidationFailed>();
        await fixture.ShouldNotHavePublishedAsync<CreateActionGroup>();
        fixture.VerifyHostOrderAdded(Times.Never());
    }

    [TestMethod]
    public async Task Consume_CreateActionGroup_NoStations_DoesNotPublishCreateActionGroup()
    {
        var orderDetails = OrderGenerator.GenerateOrderDetails(OrderGenerator.Apl1, OrderGenerator.Ps01, "hu");

        var fixture = new HostOrderCommandHandlerTestFixture()
            .WithValidationError(null)
            .WithStations([], []);

        await fixture.PublishMessageAsync(new Messages.CreateActionGroup(orderDetails.LeNo, orderDetails));

        await fixture.ShouldNotHavePublishedAsync<CreateActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<HostOrderValidationFailed>();
        fixture.VerifyHostOrderAdded(Times.Never());
    }
}