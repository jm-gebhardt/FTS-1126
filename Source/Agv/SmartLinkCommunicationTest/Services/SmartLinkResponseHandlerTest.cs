using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using SmartLinkCommunicationTest.Helpers;

namespace SmartLinkCommunicationTest.Services;

[TestClass]
public class SmartLinkResponseHandlerTest
{
    [TestMethod]
    public async Task HandleAsync_Success()
    {
        var config = await ConfigGenerator.GenerateConfigAsync();
        var moduleConfig = config.Modules.First();

        var smartLinkData = await SmartLinkDataGenerator.GenerateSmartLinkDataAsync();
        var fixture = await new SmartLinkResponseHandlerTestFixture()
            .WithModuleConfig(moduleConfig)
            .BuildAsync();

        await fixture.SmartLinkResponseHandler.HandleAsync(smartLinkData).ShouldNotThrowAsync();
    }
}