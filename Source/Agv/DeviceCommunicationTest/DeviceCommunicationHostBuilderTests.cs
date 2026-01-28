using Gebhardt.Acc.Common.Testing.Configurations;
using Gebhardt.Acc.DeviceCommunication.KarisAgv;
using Gebhardt.Acc.DeviceCommunication.SimAgv;
using Gebhardt.Acc.DeviceCommunication.SimAgv.CmdOptions;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest;

[TestClass]
public class DeviceCommunicationHostBuilderTests
{
    [TestMethod]
    [DoNotParallelize]
    public void CreateHostBuilder_ValidData_NoCaptiveDependencies()
    {
        var hostBuilder = DeviceCommunicationHostBuilder.CreateHostBuilder(["--Observability:ServiceName=service"]);

        hostBuilder.FindCaptiveDependencies().ShouldBeEmpty();
    }

    [TestMethod]
    [DoNotParallelize]
    public void CreateHostBuilderSim_ValidData_NoCaptiveDependencies()
    {
        var hostBuilder = DeviceCommunicationSimAgvHostBuilder.CreateHostBuilder(["--Observability:ServiceName=service"], new Options {FixedGraph = true}, "KARISEXPORT", "e", "s", "i", "n");
        hostBuilder.ShouldNotBeNull();
        hostBuilder.FindCaptiveDependencies().ShouldBeEmpty();
    }
}