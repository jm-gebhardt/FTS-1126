using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.SystemStatus;
using Gebhardt.Acc.PrismatCommunication.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.Acc.PrismatCommunicationTest.Services;

[TestClass]
public class AgvEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AllAgvsOffline_ValidData_PublishesSetNotReady()
    {
        using var fixture = new HostOrderConsumerTestFixture<AgvEventHandler>();

        await fixture.TestHarness.Bus.Publish(new AllAgvsOffline());

        await fixture.ShouldHaveConsumedAsync<AllAgvsOffline>();
        await fixture.ShouldHavePublishedAsync<SetNotReady>();
    }

    [TestMethod]
    public async Task Consume_AgvWentOnline_ValidData_PublishesSetReady()
    {
        using var fixture = new HostOrderConsumerTestFixture<AgvEventHandler>();

        await fixture.TestHarness.Bus.Publish(new AgvWentOnline());

        await fixture.ShouldHaveConsumedAsync<AgvWentOnline>();
        await fixture.ShouldHavePublishedAsync<SetReady>();
    }
}