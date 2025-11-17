using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.Testing.Configurations;
using Gebhardt.Acc.KaeserSidecar.CmdOptions;
using Gebhardt.Acc.KaeserSidecar.Configuration;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test;

[TestClass]
[DoNotParallelize]
public class KaeserSidecarHostBuilderTests
{
    [TestMethod]
    public void CreateHostBuilder_ValidData_NoCaptiveDependencies()
    {
        var hostBuilder = KaeserSidecarHostBuilder.CreateHostBuilder([], new SimulateOptions(), InitialLogger.Create<KaeserSidecarHostBuilderTests>());

        hostBuilder.FindCaptiveDependencies().ShouldBeEmpty();
    }
}