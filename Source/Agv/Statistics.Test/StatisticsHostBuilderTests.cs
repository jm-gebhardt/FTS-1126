using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.Testing.Configurations;
using Gebhardt.Acc.Statistics.Configuration;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test;

[TestClass]
[DoNotParallelize]
public class StatisticsHostBuilderTests
{
    [TestMethod]
    public void CreateHostBuilder_ValidData_NoCaptiveDependencies()
    {
        var hostBuilder = StatisticsHostBuilder.CreateHostBuilder([], InitialLogger.Create<StatisticsHostBuilderTests>(), "ID", "Name");

        hostBuilder.FindCaptiveDependencies().ShouldBeEmpty();
    }
}