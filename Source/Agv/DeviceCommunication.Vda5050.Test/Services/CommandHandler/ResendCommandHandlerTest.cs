using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.CommandHandler;

[TestClass]
public class ResendCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_RequestAgvData_ValidData_PublishesAgvDataReceived()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var fixture = new ResendCommandHandlerTestFixture()
            .AddAgvData(agvData);

        await fixture.PublishMessageAsync(new RequestAgvData(agvData.AgvCorrelationId, "serial number"));

        await fixture.ShouldHavePublishedAsync<AgvDataReceived>();
    }

    [TestMethod]
    public async Task Consume_RequestAgvData_NoAgvData_DoesNotPublishAgvDataReceived()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var fixture = new ResendCommandHandlerTestFixture()
            .AddAgvData(null);

        await fixture.PublishMessageAsync(new RequestAgvData(agvData.AgvCorrelationId, "serial number"));

        await fixture.ShouldNotHavePublishedAsync<AgvDataReceived>();
    }
}