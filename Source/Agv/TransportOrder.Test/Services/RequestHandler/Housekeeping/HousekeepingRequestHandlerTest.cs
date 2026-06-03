using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.Housekeeping;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Housekeeping;
using MassTransit;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.Housekeeping;

[TestClass]
public class HousekeepingRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_UpdateHousekeepingConfiguration_ValidData_RespondsSuccess()
    {
        var fixture = new HousekeepingRequestHandlerTestFixture<UpdateHousekeepingConfiguration>();

        Response<Success> response = await fixture.GetResponseAsync<Success>(new UpdateHousekeepingConfiguration(HousekeepingEntityType.Logs, 14, HousekeepingInterval.Days, 3, HousekeepingInterval.Days, 1, 1));

        response.Message.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Consume_UpdateHousekeepingConfiguration_ValidData_CallsRepositoryWithCorrectValues()
    {
        var fixture = new HousekeepingRequestHandlerTestFixture<UpdateHousekeepingConfiguration>();

        await fixture.GetResponseAsync<Success>(new UpdateHousekeepingConfiguration(HousekeepingEntityType.Logs, 7, HousekeepingInterval.Weeks, 3, HousekeepingInterval.Days, 1, 1));

        fixture.HousekeepingConfigurationRepository.Verify(
            x => x.AddOrUpdateAsync(It.Is<HousekeepingConfiguration>(c =>
                c.OutdatedValue == 7 &&
                c.OutdatedInterval == HousekeepingInterval.Weeks)),
            Times.Once);
    }
}
