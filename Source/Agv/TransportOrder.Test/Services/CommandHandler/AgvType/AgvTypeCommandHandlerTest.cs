using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.CommandHandler.AgvType;

[TestClass]
public class AgvTypeCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_NewAgvType_UpsertsAndLogsSystem()
    {
        const string agvTypeIdentifier = AgvTypeCommandHandlerTestFixture.AgvTypeIdentifier;
        var fixture = new AgvTypeCommandHandlerTestFixture();

        await fixture.PublishMessageAsync(new AddOrUpdateAgvType(
            agvTypeIdentifier.ToGuid(),
            AgvTypeGenerator.GenerateAgvTypeInfo(agvTypeIdentifier)));

        fixture.VerifyAgvTypeUpserted(Times.Once());
        fixture.VerifySystemLogged(Times.Once());
    }

    [TestMethod]
    public async Task Consume_ExistingAgvType_UpsertsButDoesNotLog()
    {
        const string agvTypeIdentifier = AgvTypeCommandHandlerTestFixture.AgvTypeIdentifier;
        var fixture = new AgvTypeCommandHandlerTestFixture().WithExistingAgvType(agvTypeIdentifier);

        await fixture.PublishMessageAsync(new AddOrUpdateAgvType(
            agvTypeIdentifier.ToGuid(),
            AgvTypeGenerator.GenerateAgvTypeInfo(agvTypeIdentifier)));

        fixture.VerifyAgvTypeUpserted(Times.Once());
        fixture.VerifySystemLogged(Times.Never());
    }
}