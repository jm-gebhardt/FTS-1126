using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Gebhardt.Acc.Statistics.Test.Models;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Services.EventHandler;

[TestClass]
public class GridCellAttachedHandlerBaseTest
{
    [TestMethod]
    public async Task Consume_WithCachedPosition_CallsHandleAsync()
    {
        var message = new TestMessage();
        var gridCell = new GridCell(1, 1, 1, 2, 1, 2, new Grid("a grid", 1, 1));

        var fixture = new GridPositionAttachedTestHandlerTestFixture()
            .WithGridCells([gridCell]);

        await fixture.Handler.Consume(GridPositionAttachedTestHandlerTestFixture.CreateContext(message));

        fixture.Handler.HandledMessages.ShouldHaveSingleItem();
        fixture.Handler.HandledMessages[0].Item1.ShouldBe(message);
        fixture.Handler.HandledMessages[0].Item2.ShouldBe([gridCell]);
    }

    [TestMethod]
    public async Task OnGridPositionPersisted_WithoutIds_CallsHandlerWithEmptyList()
    {
        var message = new TestMessage();

        var fixture = new GridPositionAttachedTestHandlerTestFixture()
            .WithGridCells([]);

        await fixture.Handler.Consume(GridPositionAttachedTestHandlerTestFixture.CreateContext(message));

        fixture.Handler.HandledMessages.ShouldHaveSingleItem();
        fixture.Handler.HandledMessages[0].Item1.ShouldBe(message);
        fixture.Handler.HandledMessages[0].Item2.ShouldBe([]);
    }
}