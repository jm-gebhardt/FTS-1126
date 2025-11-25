using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Services.EventHandler;
using Gebhardt.Acc.Statistics.Test.Extensions;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Moq;

namespace Gebhardt.Acc.Statistics.Test.Services.EventHandler;

[TestClass]
public class AgvErrorEventHandlerTest
{
    [TestMethod]
    public async Task HandleWithGridIds_PublishesAddError()
    {
        var msg = new AgvErrorReceived(AgvErrorGenerator.GenerateAgvError(), new AgvPosition(true, 1, 1, 0, 0), "agv");
        var gridCell = new GridCell(1, 1, 1, 2, 1, 2, new Grid("", 1, 1));
        GridPositionAttachedHandlerTestFixture<AgvErrorEventHandler, AgvErrorReceived> fixture = new GridPositionAttachedHandlerTestFixture<AgvErrorEventHandler, AgvErrorReceived>().Build();

        await fixture.Handler.HandleTestMessageDirectly(msg, [gridCell]);

        fixture.PublishEndpointMock.Verify(p => p.Publish(
            It.Is<AddError>(x =>
                x.Error == msg.Error &&
                x.GridCellIds.Count == 1 &&
                x.GridCellIds[0] == gridCell.CellId),
            CancellationToken.None), Times.Once);
    }
}