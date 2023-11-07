using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using SmartLinkCommunicationTest.Helpers;

namespace SmartLinkCommunicationTest.Services;

[TestClass]
public class ModulePollingServiceTest
{
    [TestMethod]
    public async Task PollAsync_Success_GetsDataFromSmartLinkServiceAndHandleResponse()
    {
        string smartLinkResponse = await SmartLinkDataGenerator.GenerateSmartLinkDataJsonAsync();
        var config = await ConfigGenerator.GenerateConfigAsync();
        var fixture = new ModulePollingServiceTestFixture()
            .WithResponse(smartLinkResponse)
            .WithModuleConfig(config.Modules.First())
            .Build();

        await fixture.ModulePollingService.Poll(CancellationToken.None).ShouldNotThrowAsync();
        
        fixture.SmartLinkService.Verify(service => service.GetDataAsync(), Times.Once);
        fixture.SmartLinkResponseHandler.Verify(handler => handler.HandleAsync(It.IsAny<SmartLinkData>()), Times.Once);
    }
}