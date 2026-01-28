using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.Testing.Configurations;
using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using Gebhardt.Acc.PrismatCommunication.Configuration;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest;

[TestClass]
[DoNotParallelize]
public class PrismatCommunicationHostBuilderTests
{
    [TestMethod]
    public void CreateHostBuilder_ValidData_NoCaptiveDependencies()
    {
        var hostBuilder = PrismatCommunicationHostBuilder.CreateHostBuilder(["--Observability:ServiceName=service"], new SimulateOptions(), InitialLogger.Create<PrismatCommunicationHostBuilderTests>());

        hostBuilder.FindCaptiveDependencies().ShouldBeEmpty();
    }
}