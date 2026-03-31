using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.Testing.Configurations;
using Gebhardt.Acc.TestPlanRunner.Configuration;
using Shouldly;

namespace TestPlanRunner.Test;

[TestClass]
[DoNotParallelize]
public class TestPlanRunnerHostBuilderTest
{
    [TestMethod]
    public void CreateHostBuilder_ValidData_NoCaptiveDependencies()
    {
        var hostBuilder = TestPlanRunnerHostBuilder.CreateHostBuilder(["--Observability:ServiceName=service"], InitialLogger.Create<TestPlanRunnerHostBuilderTest>());
        hostBuilder.FindCaptiveDependencies().ShouldBeEmpty();
    }
}