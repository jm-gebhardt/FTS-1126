using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TriggerService.Services;
using Gebhardt.Acc.TriggerService.Test.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.Acc.TriggerService.Test.Services;

[TestClass]
public class TriggerEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvReachedTrigger_ValidData_InvokeTrigger()
    {
        var fixture = new TriggerMessageConsumerTestFixture<TriggerEventHandler>();

        var message = new AgvReachedTrigger(Guid.NewGuid(), new Dictionary<string, ConfigurationParameter>(), AgvDataGenerator.GenerateAgvData(), new AgvStatus(), AgvActionGenerator.GeneratePickDropActionGroup(), CargoGenerator.GenerateCargos(Guid.NewGuid()), [AgvErrorGenerator.GenerateAgvError()]);

        await fixture.TestHarness.Bus.Publish(message);
        
        await fixture.ShouldHaveConsumedAsync<AgvReachedTrigger>();
        fixture.VerifyTriggerInvoked(message.TriggerId, message.AgvData, message.AgvStatus, message.ActionGroup, message.Cargo, message.Errors);
    }

    [TestMethod]
    public async Task Consume_TriggerConfigurationUpdated_ValidData_UpdateConfigurationParameters()
    {
        var fixture = new TriggerMessageConsumerTestFixture<TriggerEventHandler>();

        var message = new DefaultTriggerConfigurationUpdated(Guid.NewGuid(), [new ConfigurationParameter("name", "description", "value", "validation")]);
        await fixture.TestHarness.Bus.Publish(message);
        
        await fixture.ShouldHaveConsumedAsync<DefaultTriggerConfigurationUpdated>();
        fixture.VerifyConfigurationParametersUpdated(message.TriggerId, message.UpdatedParameters);
    }
}