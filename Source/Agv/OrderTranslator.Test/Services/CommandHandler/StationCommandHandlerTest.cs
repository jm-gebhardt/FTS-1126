using Gebhardt.Acc.OrderTranslator.Messages;
using Gebhardt.Acc.OrderTranslator.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.OrderTranslator.Test.Services.CommandHandler;

[TestClass]
public class StationCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_CreateStation_ValidData_StationCreated()
    {
        var fixture = new StationCommandHandlerTestFixture();

        await fixture.PublishMessageAsync(new CreateStation(OrderGenerator.Apl1));

        await fixture.StationRepository.GetStationByName(OrderGenerator.Apl1.Name).ShouldNotBeNull();
    }
}