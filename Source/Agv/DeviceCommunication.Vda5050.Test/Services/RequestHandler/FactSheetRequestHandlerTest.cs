using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.FactSheet;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.FactSheet;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Services.RequestHandler;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.RequestHandler;

[TestClass]
public class FactSheetRequestHandlerTest
{
    private static AddOrUpdateFactSheet BuildMessage(
        Guid? agvCorrelationId = null,
        string serialNumber = FactSheetRequestHandlerTestFixture.SerialNumber,
        ICollection<FactsheetAction>? agvActions = null,
        string rawJson = "")
    {
        return new AddOrUpdateFactSheet(
            agvCorrelationId ?? Guid.NewGuid(),
            new Factsheet
            {
                SerialNumber = serialNumber,
                Manufacturer = FactSheetRequestHandlerTestFixture.Manufacturer,
                TypeSpecification = new TypeSpecification { SeriesName = FactSheetRequestHandlerTestFixture.SeriesName },
                PhysicalParameters = new PhysicalParameters { Length = FactSheetRequestHandlerTestFixture.Length, Width = FactSheetRequestHandlerTestFixture.Width },
                ProtocolFeatures = new ProtocolFeatures { AgvActions = agvActions ?? [] }
            },
            rawJson);
    }

    [TestMethod]
    public async Task Consume_FactSheetWithNoActions_PublishesAgvDataReceived()
    {
        var agvCorrelationId = Guid.NewGuid();
        var fixture = new FactSheetRequestHandlerTestFixture();

        await fixture.GetResponseAsync<Success>(BuildMessage(agvCorrelationId));

        var published = await fixture.GetFirstPublishedMessageAsync<AgvDataReceived>();
        published.CorrelationId.ShouldBe(agvCorrelationId);
        published.SerialNumber.ShouldBe(FactSheetRequestHandlerTestFixture.SerialNumber);
    }

    [TestMethod]
    public async Task Consume_FactSheetWithNoActions_PublishesAgvAddedOrUpdated()
    {
        var agvCorrelationId = Guid.NewGuid();
        MediatorTestFixture<AddOrUpdateFactSheet, FactSheetRequestHandler, MockConsumer> fixture = new FactSheetRequestHandlerTestFixture();

        await fixture.GetResponseAsync<Success>(BuildMessage(agvCorrelationId));

        var published = await fixture.GetFirstPublishedMessageAsync<AgvAddedOrUpdated>();
        published.CorrelationId.ShouldBe(agvCorrelationId);
        published.SerialNumber.ShouldBe(FactSheetRequestHandlerTestFixture.SerialNumber);
    }

    [TestMethod]
    public async Task Consume_FactSheetWithNoActions_DoesNotSaveActionsOrPublishInstantActions()
    {
        var fixture = new FactSheetRequestHandlerTestFixture();

        await fixture.GetResponseAsync<Success>(BuildMessage());

        (await fixture.TestHarness.Published.Any<AgvInstantActionDefinitionReceived>()).ShouldBeFalse();
        fixture.VerifyActionsStored(FactSheetRequestHandlerTestFixture.SerialNumber, Times.Never());
    }

    [TestMethod]
    public async Task Consume_FactSheetWithActions_SavesActionsWithSerialNumber()
    {
        const string serialNumber = "SN-002";
        var actions = new List<FactsheetAction>
        {
            new() { ActionType = "pick", ActionScopes = [ActionScope.Node] }
        };
        var fixture = new FactSheetRequestHandlerTestFixture();

        await fixture.GetResponseAsync<Success>(BuildMessage(serialNumber: serialNumber, agvActions: actions));

        fixture.VerifyActionsStored(serialNumber, Times.Once());
    }

    [TestMethod]
    public async Task Consume_FactSheetWithInstantAction_PublishesAgvInstantActionDefinitionReceived()
    {
        var agvCorrelationId = Guid.NewGuid();
        var actions = new List<FactsheetAction>
        {
            new() { ActionType = "startPause", ActionScopes = [ActionScope.Instant] }
        };
        var fixture = new FactSheetRequestHandlerTestFixture();

        await fixture.GetResponseAsync<Success>(BuildMessage(agvCorrelationId, agvActions: actions));

        var published = await fixture.GetFirstPublishedMessageAsync<AgvInstantActionDefinitionReceived>();
        published.AgvCorrelationId.ShouldBe(agvCorrelationId);
        published.InstantActionDefinition.ActionType.ShouldBe("startPause");
    }

    [TestMethod]
    public async Task Consume_FactSheetWithNonInstantAction_DoesNotPublishAgvInstantActionDefinitionReceived()
    {
        var actions = new List<FactsheetAction>
        {
            new() { ActionType = "pick", ActionScopes = [ActionScope.Node] }
        };
        MediatorTestFixture<AddOrUpdateFactSheet, FactSheetRequestHandler, MockConsumer> fixture = new FactSheetRequestHandlerTestFixture();

        await fixture.GetResponseAsync<Success>(BuildMessage(agvActions: actions));

        (await fixture.TestHarness.Published.Any<AgvInstantActionDefinitionReceived>()).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_FactSheetWhenExceptionOccurs_ThrowsInvalidFactsheetException()
    {
        var actions = new List<FactsheetAction>
        {
            new() { ActionType = "pick", ActionScopes = [ActionScope.Node] }
        };
        MediatorTestFixture<AddOrUpdateFactSheet, FactSheetRequestHandler, MockConsumer> fixture = new FactSheetRequestHandlerTestFixture()
            .WithAddOrUpdateActionsThrows();

        await MediatorShould.ThrowRequestExceptionAsync<InvalidFactsheetException>(async () => await fixture.GetResponseAsync<Success>(BuildMessage(agvActions: actions)));
    }

    [TestMethod]
    public async Task Consume_FactSheet_StoresAcknowledgedAgvTypeWithRawData()
    {
        const string rawJson = """{"manufacturer":"TestManufacturer","physicalParameters":{"length":1.5}}""";
        var fixture = new FactSheetRequestHandlerTestFixture();

        await fixture.GetResponseAsync<Success>(BuildMessage(rawJson: rawJson));

        fixture.AddedAgvType.ShouldNotBeNull();
        fixture.AddedAgvType.AgvTypeIdentifier.ShouldBe(FactSheetRequestHandlerTestFixture.AgvTypeIdentifier);
        fixture.AddedAgvType.Length.ShouldBe(FactSheetRequestHandlerTestFixture.Length);
        fixture.AddedAgvType.Width.ShouldBe(FactSheetRequestHandlerTestFixture.Width);
        fixture.AddedAgvType.RawData.ShouldBe(rawJson);
        fixture.AddedAgvType.Acknowledged.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvTypeAlreadyExists_ThrowsAgvTypeAlreadyExistsException()
    {
        var fixture = FactSheetRequestHandlerTestFixture.WithExistingAgvType();

        await MediatorShould.ThrowRequestExceptionAsync<AgvTypeAlreadyExistsException>(async () => await fixture.GetResponseAsync<Success>(BuildMessage()));
    }
}