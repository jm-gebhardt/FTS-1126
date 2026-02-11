using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Services.CommandHandler;
using Gebhardt.Acc.Statistics.Test.Fixtures;

namespace Gebhardt.Acc.Statistics.Test.Services.CommandHandler;

[TestClass]
public class GridCommandHandlerTest
{
    [TestMethod]
    public async Task ConsumeAddGrid_ValidData_GridAdded()
    {
        var fixture = new MockDbConsumerTestFixture<GridCommandHandler>();
        await fixture.TestHarness.Bus.Publish(new AddGrid("gridName", 1, 1));
        await fixture.ShouldHaveConsumedAsync<AddGrid>();
        fixture.StatisticsDbMock.VerifyGridAdded();
    }
}