using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Mqtt;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.MqttMessageHandler;

[TestClass]
public class MqttFactSheetConsumerTest
{
    [TestMethod]
    public async Task Consume_MqttFactSheet_ValidData_PublishesAgvDataReceived()
    {
        var fixture = new MqttFactSheetConsumerTestFixture();

        var message = MqttFactSheetGenerator.Generate(actions: [MqttFactSheetGenerator.GenerateAgvAction()]);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<AgvDataReceived>();

        fixture.VerifyActionsUpdated(Times.Once());
        fixture.VerifyDataUpdated();
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_NoActions_PublishesAgvDataReceived()
    {
        var fixture = new MqttFactSheetConsumerTestFixture();

        var message = MqttFactSheetGenerator.Generate(actions: null);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<AgvDataReceived>();

        fixture.VerifyActionsUpdated(Times.Never());
        fixture.VerifyDataUpdated();
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_InstantAction_PublishesAgvInstantActionDefinitionReceived()
    {
        var fixture = new MqttFactSheetConsumerTestFixture();

        var message = MqttFactSheetGenerator.Generate(actions: [MqttFactSheetGenerator.GenerateAgvAction("initToStation", MqttActionScope.Instant)]);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<AgvDataReceived>();
        await fixture.ShouldHavePublishedAsync<AgvInstantActionDefinitionReceived>();

        fixture.VerifyActionsUpdated(Times.Once());
        fixture.VerifyDataUpdated();
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_NewAgvType_AddsUnacknowledgedTypeAndPublishesAgvTypeCreated()
    {
        var fixture = new MqttFactSheetConsumerTestFixture();
        var message = MqttFactSheetGenerator.Generate();

        await fixture.PublishMqttMessageAsync(message);

        fixture.VerifyAgvTypeAdded(Times.Once());
        var created = await fixture.GetFirstPublishedMessageAsync<AgvTypeCreated>();
        created.AgvType.AgvTypeIdentifier.ShouldBe($"{message.Manufacturer}.{message.TypeSpecification.SeriesName}");
        created.AgvType.Acknowledged.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_ExistingAgvType_DoesNotAddOrPublishAgvTypeCreated()
    {
        var fixture = new MqttFactSheetConsumerTestFixture().WithExistingAgvType();
        var message = MqttFactSheetGenerator.Generate();

        await fixture.PublishMqttMessageAsync(message);

        fixture.VerifyAgvTypeAdded(Times.Never());
        await fixture.ShouldNotHavePublishedAsync<AgvTypeCreated>();
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_NewAgv_CompatibleExistingType_PublishesAgvDiscoveredWithValidConfiguration()
    {
        var fixture = new MqttFactSheetConsumerTestFixture().WithExistingAgvType();
        var message = MqttFactSheetGenerator.Generate();

        await fixture.PublishMqttMessageAsync(message);

        var discovered = await fixture.GetFirstPublishedMessageAsync<AgvDiscovered>();
        discovered.HasValidConfiguration.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_NewAgv_IncompatibleExistingType_PublishesAgvDiscoveredWithInvalidConfiguration()
    {
        var fixture = new MqttFactSheetConsumerTestFixture().WithExistingAgvType(length: 2.0);
        var message = MqttFactSheetGenerator.Generate();

        await fixture.PublishMqttMessageAsync(message);

        var discovered = await fixture.GetFirstPublishedMessageAsync<AgvDiscovered>();
        discovered.HasValidConfiguration.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_ExistingAgvNotPublishedToFleetManager_PublishesAgvTypeChangedWithValidConfiguration()
    {
        var fixture = new MqttFactSheetConsumerTestFixture()
            .WithExistingAgv(publishedToFleetManager: false)
            .WithExistingAgvType();
        var message = MqttFactSheetGenerator.Generate();

        await fixture.PublishMqttMessageAsync(message);

        var typeChanged = await fixture.GetFirstPublishedMessageAsync<AgvTypeChanged>();
        typeChanged.HasValidConfiguration.ShouldBeTrue();
        typeChanged.NeedsConfirmation.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_ExistingAgvNotPublishedToFleetManager_IncompatibleExistingType_PublishesAgvTypeChangedWithInvalidConfiguration()
    {
        var fixture = new MqttFactSheetConsumerTestFixture()
            .WithExistingAgv(publishedToFleetManager: false)
            .WithExistingAgvType(length: 2.0);
        var message = MqttFactSheetGenerator.Generate();

        await fixture.PublishMqttMessageAsync(message);

        var typeChanged = await fixture.GetFirstPublishedMessageAsync<AgvTypeChanged>();
        typeChanged.HasValidConfiguration.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_NoTypeChange_CompatibleType_PublishesNothing()
    {
        var fixture = new MqttFactSheetConsumerTestFixture()
            .WithExistingAgv()
            .WithExistingAgvType();
        var message = MqttFactSheetGenerator.Generate();

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldNotHavePublishedAsync<AgvTypeChanged>();
        await fixture.ShouldNotHavePublishedAsync<AgvDiscovered>();
        await fixture.ShouldNotHavePublishedAsync<InvalidAgvConfigurationReceived>();
        await fixture.ShouldNotHavePublishedAsync<AgvDataReceived>();
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_NoTypeChange_IncompatibleType_PublishesInvalidAgvConfigurationReceived()
    {
        var fixture = new MqttFactSheetConsumerTestFixture()
            .WithExistingAgv()
            .WithExistingAgvType(length: 2.0);
        var message = MqttFactSheetGenerator.Generate();

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<InvalidAgvConfigurationReceived>();
        await fixture.ShouldNotHavePublishedAsync<AgvTypeChanged>();
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_TypeChanged_NewTypeUnknown_PublishesAgvTypeChangedNeedingConfirmation()
    {
        var fixture = new MqttFactSheetConsumerTestFixture()
            .WithExistingAgv(manufacturer: "oldManufacturer", seriesName: "oldSeries");
        var message = MqttFactSheetGenerator.Generate();

        await fixture.PublishMqttMessageAsync(message);

        var typeChanged = await fixture.GetFirstPublishedMessageAsync<AgvTypeChanged>();
        typeChanged.HasValidConfiguration.ShouldBeFalse();
        typeChanged.NeedsConfirmation.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_TypeChanged_IncompatibleWithPriorType_PublishesAgvTypeChangedInvalid()
    {
        var fixture = new MqttFactSheetConsumerTestFixture()
            .WithExistingAgv(manufacturer: "manufacturer", seriesName: "series")
            .WithExistingAgvType(manufacturer: "manufacturer", seriesName: "series")
            .WithExistingAgvType();
        var message = MqttFactSheetGenerator.Generate(seriesName: "series", width:2);

        await fixture.PublishMqttMessageAsync(message);

        var typeChanged = await fixture.GetFirstPublishedMessageAsync<AgvTypeChanged>();
        typeChanged.HasValidConfiguration.ShouldBeFalse();
        typeChanged.NeedsConfirmation.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_TypeChanged_CompatibleWithPriorType_PublishesAgvTypeChangedValid()
    {
        var fixture = new MqttFactSheetConsumerTestFixture()
            .WithExistingAgv(manufacturer: "oldManufacturer", seriesName: "oldSeries")
            .WithExistingAgvType(lookupIdentifier: "oldManufacturer.oldSeries")
            .WithExistingAgvType();
        var message = MqttFactSheetGenerator.Generate();

        await fixture.PublishMqttMessageAsync(message);

        var typeChanged = await fixture.GetFirstPublishedMessageAsync<AgvTypeChanged>();
        typeChanged.HasValidConfiguration.ShouldBeTrue();
        typeChanged.NeedsConfirmation.ShouldBeFalse();
    }
}
