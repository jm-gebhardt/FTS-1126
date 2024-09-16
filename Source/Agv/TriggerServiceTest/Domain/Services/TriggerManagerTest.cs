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

        var configuration = new ConfigurationParameter("test", "description", "value", "");
        fixture.TestTrigger.Parameters.TryAdd(configuration.Name, configuration);

        const string updatedValue = "new value";
        var parameter = new ConfigurationParameter(configuration.Name, configuration.Description, updatedValue, configuration.Validation);
        await fixture.TriggerManager.UpdateDefaultConfigurationParameters(fixture.TestTrigger.Properties.Id, [parameter]);

        fixture.TestTrigger.Parameters[configuration.Name].Value.ShouldBe(updatedValue);
        fixture.VerifyTriggerUpdated();
    }
}