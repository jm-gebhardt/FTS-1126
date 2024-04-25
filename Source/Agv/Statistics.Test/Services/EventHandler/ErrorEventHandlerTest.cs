using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Services.EventHandler;

[TestClass]
public class ErrorEventHandlerTest
{
    [TestMethod]
    public async Task ConsumeAgvErrorReceived_ValidData_PublishesAddError()
    {
        var grid = new Grid("1x1", 12, 12, 1, 1, 1, 1);
        var position = new GridAgvPosition(grid.GridId, "agv", 1, 1, 1, 1, 1, DateTime.UtcNow, grid);
        MockDbConsumerTestFixture<Statistics.Services.EventHandler.ErrorEventHandler> fixture = new MockDbConsumerTestFixture<Statistics.Services.EventHandler.ErrorEventHandler>()
            .WithGrids([grid])
            .WithGridPositions([position]);
        var error = AgvErrorGenerator.GenerateAgvError();
        await fixture.TestHarness.Bus.Publish(new AgvErrorReceived(error, new AgvPosition(true, position.X, position.Y, 1,0), position.SerialNumber));
        (await fixture.ConsumerHarness.Consumed.Any<AgvErrorReceived>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<AddError>()).ShouldBeTrue();
    }
}