using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.Testing.Configurations;
using Gebhardt.Acc.OrderTranslator.CmdOptions;
using Gebhardt.Acc.OrderTranslator.Configuration;
using Shouldly;

namespace Gebhardt.Acc.OrderTranslator.Test;

[TestClass]
[DoNotParallelize]
public class OrderTranslatorHostBuilderTests
{
    [TestMethod]
    public void CreateHostBuilder_ValidData_NoCaptiveDependencies()
    {
        var hostBuilder = OrderTranslatorHostBuilder.CreateHostBuilder([], new SimulateOptions(), InitialLogger.Create<OrderTranslatorHostBuilderTests>());

        hostBuilder.FindCaptiveDependencies().ShouldBeEmpty();
    }
}