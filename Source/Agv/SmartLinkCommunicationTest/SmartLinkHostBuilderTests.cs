using Gebhardt.Acc.Common.Testing.Configurations;
using Gebhardt.Acc.SmartLinkCommunication;
using Gebhardt.Acc.SmartLinkCommunication.Models;
using Shouldly;

namespace Gebhardt.Acc.SmartLinkCommunicationTest;

[TestClass]
[DoNotParallelize]
public class SmartLinkHostBuilderTests
{
    [TestMethod]
    public void CreateHostBuilder_ValidData_NoCaptiveDependencies()
    {
        var hostBuilder = SmartLinkHostBuilder.CreateHostBuilder([], new Config { Modules = [new ModuleConfig("", [new IoActionConfig("id", TimeSpan.MinValue, false, ActionTypes.PublishQueueActionGroup, [])])] });

        hostBuilder.FindCaptiveDependencies().ShouldBeEmpty();
    }
}