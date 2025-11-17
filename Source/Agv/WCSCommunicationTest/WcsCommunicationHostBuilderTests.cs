using Gebhardt.Acc.Common.Testing.Configurations;
using Gebhardt.Acc.WCSCommunication;
using Gebhardt.Acc.WCSCommunication.CmdOptions;
using Shouldly;

namespace Gebhardt.Acc.WCSCommunicationTest;

[TestClass]
[DoNotParallelize]
public class WcsCommunicationHostBuilderTests
{
    [TestMethod]
    public void CreateHostBuilder_ValidData_NoCaptiveDependencies()
    {
        var hostBuilder = WcsCommunicationHostBuilder.CreateHostBuilder([], true, new SimulateOptions());

        hostBuilder.FindCaptiveDependencies().ShouldBeEmpty();
    }
}