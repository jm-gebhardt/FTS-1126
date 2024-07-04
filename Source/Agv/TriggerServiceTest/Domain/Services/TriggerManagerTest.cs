using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TriggerService.Test.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TriggerService.Test.Domain.Services;

[TestClass]
public class TriggerManagerTest
{
    [TestMethod]
    public async Task UpdateConfigurationParameters_ValidData_UpdatedConfigurationParameters()
    {
        var fixture = new TriggerManagerTestFixture();

        var configuration = new ConfigurationParameter("test", "value", "");
        fixture.TestTrigger.Parameters.TryAdd(configuration.Name, configuration);

        const string updatedValue = "new value";
        await fixture.TriggerManager.UpdateConfigurationParameters(fixture.TestTrigger.Properties.Id, [new ConfigurationParameter(configuration.Name, updatedValue, configuration.Validation)]);

        fixture.TestTrigger.Parameters[configuration.Name].Value.ShouldBe(updatedValue);
        fixture.VerifyTriggerUpdated();
    }
}