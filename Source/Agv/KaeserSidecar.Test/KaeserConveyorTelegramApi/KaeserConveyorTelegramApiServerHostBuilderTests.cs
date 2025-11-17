using Gebhardt.Acc.Common.Testing.Configurations;
using Gebhardt.Acc.KaeserConveyorTelegramApiServer.Configuration;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.KaeserConveyorTelegramApi;

[TestClass]
public class KaeserConveyorTelegramApiServerHostBuilderTests
{
    [TestMethod]
    public void CreateHostBuilder_ValidData_NoCaptiveDependencies()
    {
        var hostBuilder = KaeserConveyorTelegramApiServerHostBuilder.CreateHostBuilder(["--APLWESTATION", "", "--FROMWESTATION", "", "--AGVCAROSTATE", ""]);

        hostBuilder.FindCaptiveDependencies().ShouldBeEmpty();
    }
}