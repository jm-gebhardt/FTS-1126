using Gebhardt.Acc.Common.Logging;
using Gebhardt.Acc.Common.Testing.Configurations;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Configuration;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test;

[TestClass]
public class HostBuilderTest
{
    [TestMethod]
    public void CreateHostBuilder_ValidData_NoCaptiveDependencies()
    {
        var hostBuilder = DeviceCommunicationVda5050HostBuilder.CreateHostBuilder(["--Observability:ServiceName=service"], InitialLogger.Create<HostBuilderTest>());
        hostBuilder.FindCaptiveDependencies().ShouldBeEmpty();
    }
}