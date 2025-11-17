using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.Testing.Configurations;
using Gebhardt.Acc.TransportOrder.Core.CmdOptions;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test;

[TestClass]
public class TransportOrderHostBuilderTests
{
    [TestMethod]
    public void CreateHostBuilder_ValidData_NoCaptiveDependencies()
    {
        var hostBuilder = TransportOrderHostBuilder.CreateHostBuilder([], new SimulationOptions(), InitialLogger.Create<TransportOrderHostBuilderTests>(), true);

        hostBuilder.FindCaptiveDependencies().ShouldBeEmpty();
    }
}