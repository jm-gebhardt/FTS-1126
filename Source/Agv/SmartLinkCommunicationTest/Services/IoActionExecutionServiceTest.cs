using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.SmartLinkCommunication.Models;
using Gebhardt.Acc.SmartLinkCommunicationTest.Helpers;
using Moq;

namespace Gebhardt.Acc.SmartLinkCommunicationTest.Services;

[TestClass]
public class IoActionExecutionServiceTest
{
    [TestMethod]
    public async Task ReceiveIoActionConfig_WithActionTypePublishQueueActionGroup_PublishesQueueActionGroup()
    {
        var config = await ConfigGenerator.GenerateConfigAsync();
        var ioActionConfig = config.Modules.First().IoActionConfigs.First();
        var actionDefinitions = ioActionConfig.Parameters.Cast<ActionDefinition>().ToList();
        var fixture = new IoActionExecutionServiceTestFixture();

        await fixture.IoActionExecutionService.ExecuteActionAsync(ioActionConfig);

        fixture.Bus.Verify(x => x.Publish(It.Is<QueueActionGroup>(g
                => g.ActionGroup.Actions.All(a => actionDefinitions.Contains(new ActionDefinition(a.ActionType, a.Target.Node!.Name))))
            , It.IsAny<CancellationToken>()), Times.Once);
    }
}