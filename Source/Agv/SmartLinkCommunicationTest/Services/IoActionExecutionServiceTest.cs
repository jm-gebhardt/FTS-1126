using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.SmartLinkCommunication.Models;
using Gebhardt.StoreWare.Agv.SmartLinkCommunicationTest.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.StoreWare.Agv.SmartLinkCommunicationTest.Services;

[TestClass]
public class IoActionExecutionServiceTest
{
    [TestMethod]
    public async Task ReceiveIoActionConfig_WithActionTypePublishQueueActionGroup_PublishesQueueActionGroup()
    {
        var config = await ConfigGenerator.GenerateConfigAsync();
        var ioActionConfig = config.Modules.First().IoActionConfigs.First();
        List<ActionDefinition> actionDefinitions = ioActionConfig.Parameters.Cast<ActionDefinition>().ToList();
        var fixture = new IoActionExecutionServiceTestFixture().Build();

        await fixture.IoActionExecutionService.ExecuteActionAsync(ioActionConfig);

        fixture.Bus.Verify(x => x.Publish(It.Is<QueueActionGroup>(g
                => g.ActionGroup.Actions.All(a => actionDefinitions.Contains(new ActionDefinition(a.ActionType, a.TargetNodeId))))
            , It.IsAny<CancellationToken>()), Times.Once);
    }
}