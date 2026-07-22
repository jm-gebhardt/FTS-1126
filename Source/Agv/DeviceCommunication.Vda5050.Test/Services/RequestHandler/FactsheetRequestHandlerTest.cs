using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.RequestHandler;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.RequestHandler;

[TestClass]
public class FactsheetRequestHandlerTest
{
    private static AddOrUpdateFactsheet BuildMessage(
        Guid? agvCorrelationId = null,
        string serialNumber = FactsheetRequestHandlerTestFixture.SerialNumber,
        ICollection<FactsheetAction>? agvActions = null,
        string rawJson = "")
    {
        return new AddOrUpdateFactsheet(
            agvCorrelationId ?? Guid.NewGuid(),
            new Factsheet
            {
                SerialNumber = serialNumber,
                Manufacturer = FactsheetRequestHandlerTestFixture.Manufacturer,
                TypeSpecification = new TypeSpecification { SeriesName = FactsheetRequestHandlerTestFixture.SeriesName },
                PhysicalParameters = new PhysicalParameters { Length = FactsheetRequestHandlerTestFixture.Length, Width = FactsheetRequestHandlerTestFixture.Width },
                ProtocolFeatures = new ProtocolFeatures { AgvActions = agvActions ?? [] }
            },
            rawJson);
    }

    [TestMethod]
    public async Task Consume_FactsheetWithNoActions_PublishesAgvDataReceived()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new FactsheetRequestHandlerTestFixture();

        await fixture.GetResponseAsync<Success>(BuildMessage(agvCorrelationId));

        var published = await fixture.GetFirstPublishedMessageAsync<AgvDataReceived>();
        published.CorrelationId.ShouldBe(agvCorrelationId);
        published.SerialNumber.ShouldBe(FactsheetRequestHandlerTestFixture.SerialNumber);
    }

    [TestMethod]
    public async Task Consume_FactsheetWithNoActions_PublishesAgvAddedOrUpdated()
    {
        var agvCorrelationId = Guid.NewGuid();
        MediatorTestFixture<AddOrUpdateFactsheet, FactsheetRequestHandler, MockConsumer> fixture = new FactsheetRequestHandlerTestFixture();

        await fixture.GetResponseAsync<Success>(BuildMessage(agvCorrelationId));

        var published = await fixture.GetFirstPublishedMessageAsync<AgvAddedOrUpdated>();
        published.CorrelationId.ShouldBe(agvCorrelationId);
        published.SerialNumber.ShouldBe(FactsheetRequestHandlerTestFixture.SerialNumber);
    }

    [TestMethod]
    public async Task Consume_FactsheetWithNoActions_DoesNotSaveActionsOrPublishInstantActions()
    {
        var fixture = new FactsheetRequestHandlerTestFixture();

        await fixture.GetResponseAsync<Success>(BuildMessage());

        (await fixture.TestHarness.Published.Any<AgvInstantActionDefinitionReceived>()).ShouldBeFalse();
        fixture.VerifyActionsStored(FactsheetRequestHandlerTestFixture.SerialNumber, Times.Never());
    }

    [TestMethod]
    public async Task Consume_FactsheetWithActions_SavesActionsWithSerialNumber()
    {
        const string serialNumber = "SN-002";
        var actions = new List<FactsheetAction>
        {
            new() { ActionType = "pick", ActionScopes = [ActionScope.Node] }
        };
        var fixture = new FactsheetRequestHandlerTestFixture();

        await fixture.GetResponseAsync<Success>(BuildMessage(serialNumber: serialNumber, agvActions: actions));

        fixture.VerifyActionsStored(serialNumber, Times.Once());
    }

    [TestMethod]
    public async Task Consume_FactsheetWithInstantAction_PublishesAgvInstantActionDefinitionReceived()
    {
        var agvCorrelationId = Guid.NewGuid();
        var actions = new List<FactsheetAction>
        {
            new() { ActionType = "startPause", ActionScopes = [ActionScope.Instant] }
        };
        var fixture = new FactsheetRequestHandlerTestFixture();

        await fixture.GetResponseAsync<Success>(BuildMessage(agvCorrelationId, agvActions: actions));

        var published = await fixture.GetFirstPublishedMessageAsync<AgvInstantActionDefinitionReceived>();
        published.AgvCorrelationId.ShouldBe(agvCorrelationId);
        published.InstantActionDefinition.ActionType.ShouldBe("startPause");
    }

    [TestMethod]
    public async Task Consume_FactsheetWithNonInstantAction_DoesNotPublishAgvInstantActionDefinitionReceived()
    {
        var actions = new List<FactsheetAction>
        {
            new() { ActionType = "pick", ActionScopes = [ActionScope.Node] }
        };
        MediatorTestFixture<AddOrUpdateFactsheet, FactsheetRequestHandler, MockConsumer> fixture = new FactsheetRequestHandlerTestFixture();

        await fixture.GetResponseAsync<Success>(BuildMessage(agvActions: actions));

        (await fixture.TestHarness.Published.Any<AgvInstantActionDefinitionReceived>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_FactsheetWhenExceptionOccurs_ThrowsInvalidFactsheetException()
    {
        var actions = new List<FactsheetAction>
        {
            new() { ActionType = "pick", ActionScopes = [ActionScope.Node] }
        };
        MediatorTestFixture<AddOrUpdateFactsheet, FactsheetRequestHandler, MockConsumer> fixture = new FactsheetRequestHandlerTestFixture()
            .WithAddOrUpdateActionsThrows();

        await MediatorShould.ThrowRequestExceptionAsync<InvalidFactsheetException>(async () => await fixture.GetResponseAsync<Success>(BuildMessage(agvActions: actions)));
    }

    [TestMethod]
    public async Task Consume_Factsheet_StoresAcknowledgedAgvTypeWithRawData()
    {
        const string rawJson = """{"manufacturer":"TestManufacturer","physicalParameters":{"length":1.5}}""";
        var fixture = new FactsheetRequestHandlerTestFixture();

        await fixture.GetResponseAsync<Success>(BuildMessage(rawJson: rawJson));

        fixture.AddedAgvType.ShouldNotBeNull();
        fixture.AddedAgvType.AgvTypeIdentifier.ShouldBe(FactsheetRequestHandlerTestFixture.AgvTypeIdentifier);
        fixture.AddedAgvType.Length.ShouldBe(FactsheetRequestHandlerTestFixture.Length);
        fixture.AddedAgvType.Width.ShouldBe(FactsheetRequestHandlerTestFixture.Width);
        fixture.AddedAgvType.RawData.ShouldBe(rawJson);
        fixture.AddedAgvType.Acknowledged.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvTypeAlreadyExists_ThrowsAgvTypeAlreadyExistsException()
    {
        var fixture = FactsheetRequestHandlerTestFixture.WithExistingAgvType();

        await MediatorShould.ThrowRequestExceptionAsync<AgvTypeAlreadyExistsException>(async () => await fixture.GetResponseAsync<Success>(BuildMessage()));
    }
}