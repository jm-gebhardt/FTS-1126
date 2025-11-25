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
public class WifiMetricsEventHandlerTest
{
    [TestMethod]
    public async Task HandleWithGridIds_PublishesAddWifiMetrics()
    {
        var msg = new AgvWifiMetricsReceived(Guid.NewGuid(), "agv", new AgvPosition(true, 1, 1, 0, 0), WifiMetricsGenerator.GenerateWifiMetrics());
        var gridCell = new GridCell(1, 1, 1, 2, 1, 2, new Grid("", 1, 1));
        GridPositionAttachedHandlerTestFixture<WifiMetricsEventHandler, AgvWifiMetricsReceived> fixture = new WifiMetricsEventHandlerTestFixture()
            .FilterReturns(true)
            .Build();

        await fixture.Handler.HandleTestMessageDirectly(msg, [gridCell]);

        fixture.PublishEndpointMock.Verify(p => p.Publish(
            It.Is<AddWifiMetrics>(x =>
                x.Metrics == msg.Metrics &&
                x.GridCellIds.Count == 1 &&
                x.GridCellIds[0] == gridCell.CellId),
            CancellationToken.None), Times.Once);
    }

    [TestMethod]
    public async Task HandleWithGridIds_FilterReturnsFalse_DoesNotPublishAddWifiMetrics()
    {
        var msg = new AgvWifiMetricsReceived(Guid.NewGuid(), "agv", new AgvPosition(true, 1, 1, 0, 0), WifiMetricsGenerator.GenerateWifiMetrics());
        var gridCell = new GridCell(1, 1, 1, 2, 1, 2, new Grid("", 1, 1));

        GridPositionAttachedHandlerTestFixture<WifiMetricsEventHandler, AgvWifiMetricsReceived> fixture = new WifiMetricsEventHandlerTestFixture()
            .FilterReturns(false)
            .Build();

        await fixture.Handler.HandleTestMessageDirectly(msg, [gridCell]);

        fixture.PublishEndpointMock.Verify(p => p.Publish(It.IsAny<AddWifiMetrics>(), CancellationToken.None), Times.Never);
    }
}