using Gebhardt.Acc.Common.Testing.Configurations;
using Gebhardt.Acc.ConveyorTelegramApiServer.Configuration;
using Shouldly;

namespace Gebhardt.Acc.StationCommunication.Test.ConveyorTelegramApi;

[TestClass]
public class ConveyorTelegramApiServerHostBuilderTests
{
    [TestMethod]
    public void CreateHostBuilder_ValidData_NoCaptiveDependencies()
    {
        var hostBuilder = ConveyorTelegramApiServerHostBuilder.CreateHostBuilder(["--APLWESTATION", "", "--FROMWESTATION", "", "--AGVCARGOSTATE", ""]);

        hostBuilder.FindCaptiveDependencies().ShouldBeEmpty();
    }
}