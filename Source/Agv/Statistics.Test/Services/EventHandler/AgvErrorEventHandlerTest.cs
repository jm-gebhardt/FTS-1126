using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Services.EventHandler;
using Gebhardt.Acc.Statistics.Test.Extensions;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.Statistics.Test.Services.EventHandler;

[TestClass]
public class AgvErrorEventHandlerTest
{
    [TestMethod]
    public async Task HandleWithGridIds_PublishesAddError()
    {
        var msg = new AgvErrorReceived(AgvErrorGenerator.GenerateAgvError(), new AgvPosition(true, 1, 1, 0, 0), "agv");
        var gridIds = new List<Guid> { Guid.NewGuid() };
        var fixture = new GridPositionAttachedHandlerTestFixture<AgvErrorEventHandler, AgvErrorReceived>();

        await fixture.Handler.HandleTestMessageDirectly(msg, gridIds);

        fixture.PublishEndpointMock.Verify(p => p.Publish(
            It.Is<AddError>(x =>
                x.Error == msg.Error &&
                x.GridPositionIds.SequenceEqual(gridIds)),
            CancellationToken.None), Times.Once);
    }
}