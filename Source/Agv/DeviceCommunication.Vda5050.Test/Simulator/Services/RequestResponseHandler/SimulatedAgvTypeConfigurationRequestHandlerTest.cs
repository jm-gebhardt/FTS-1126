using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Common.Messages;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Requests;
using Shouldly;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services.RequestResponseHandler.SimulatedAgvTypeConfigurationRequestHandlerTestFixture;
using Generator = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator.SimulatedAgvTypeConfigurationGenerator;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services.RequestResponseHandler;

[TestClass]
public class SimulatedAgvTypeConfigurationRequestHandlerTest
{
    [TestMethod]
    public async Task Consume_RequestAddAgvTypeConfiguration_ValidData_RespondsWithConfiguration()
    {
        var fixture = new Fixture().Build();

        var response = await fixture.GetResponseAsync<AgvTypeConfigurationResponse>(
            new RequestAddAgvTypeConfiguration(Guid.NewGuid(), Fixture.AgvTypeIdentifier, new AgvTypeSimulationSettings { NoOfAgvs = Fixture.NoOfAgvs }));

        response.Message.Configuration.AgvTypeIdentifier.ShouldBe(Fixture.AgvTypeIdentifier);
        response.Message.Configuration.NoOfAgvs.ShouldBe(Fixture.NoOfAgvs);
    }

    [TestMethod]
    public async Task Consume_RequestAddAgvTypeConfiguration_ValidData_StoresConfiguration()
    {
        var fixture = new Fixture().Build();

        await fixture.GetResponseAsync<AgvTypeConfigurationResponse>(
            new RequestAddAgvTypeConfiguration(Guid.NewGuid(), Fixture.AgvTypeIdentifier, new AgvTypeSimulationSettings { NoOfAgvs = Fixture.NoOfAgvs }));

        var added = fixture.AddedConfiguration.ShouldNotBeNull();
        added.AgvTypeIdentifier.ShouldBe(Fixture.AgvTypeIdentifier);
        added.NoOfAgvs.ShouldBe(Fixture.NoOfAgvs);
    }

    [TestMethod]
    public async Task Consume_RequestAddAgvTypeConfiguration_ValidData_SpawnsAgvsWithoutCurrentConfiguration()
    {
        var fixture = new Fixture().Build();

        await fixture.GetResponseAsync<AgvTypeConfigurationResponse>(
            new RequestAddAgvTypeConfiguration(Guid.NewGuid(), Fixture.AgvTypeIdentifier, new AgvTypeSimulationSettings { NoOfAgvs = Fixture.NoOfAgvs }));

        fixture.AppliedCurrentConfiguration.ShouldBeNull();
        fixture.AppliedNewConfiguration.ShouldNotBeNull().NoOfAgvs.ShouldBe(Fixture.NoOfAgvs);
    }

    [TestMethod]
    public async Task Consume_RequestAddAgvTypeConfiguration_OmittedSettings_StoresDefaultSettings()
    {
        var fixture = new Fixture().Build();

        var response = await fixture.GetResponseAsync<AgvTypeConfigurationResponse>(
            new RequestAddAgvTypeConfiguration(Guid.NewGuid(), Fixture.AgvTypeIdentifier, new AgvTypeSimulationSettings()));

        response.Message.Configuration.NoOfAgvs.ShouldBe(Fixture.NoAgvs);
        response.Message.Configuration.StateUpdateIntervalMs.ShouldBe(SimulatedAgvTypeConfiguration.DefaultStateUpdateIntervalMs);
        response.Message.Configuration.BatteryChargeRate.ShouldBe(SimulatedAgvTypeConfiguration.DefaultBatteryChargeRate);
    }

    [TestMethod]
    public async Task Consume_RequestAddAgvTypeConfiguration_KnownAgvType_ThrowsAgvTypeConfigurationAlreadyExistsException()
    {
        var fixture = new Fixture().WithStoredConfiguration(Generator.Generate()).Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvTypeConfigurationAlreadyExistsException>(async () =>
            await fixture.GetResponseAsync<AgvTypeConfigurationResponse>(
                new RequestAddAgvTypeConfiguration(Guid.NewGuid(), Fixture.AgvTypeIdentifier, new AgvTypeSimulationSettings())));

        fixture.AddedConfiguration.ShouldBeNull();
        fixture.FleetApplied.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_RequestAddAgvTypeConfiguration_InvalidAgvTypeIdentifier_ThrowsValidationException()
    {
        var fixture = new Fixture().Build();

        await MediatorShould.ThrowRequestExceptionAsync<ValidationException>(async () =>
            await fixture.GetResponseAsync<AgvTypeConfigurationResponse>(
                new RequestAddAgvTypeConfiguration(Guid.NewGuid(), Fixture.IdentifierWithoutSeparator, new AgvTypeSimulationSettings())));

        fixture.AddedConfiguration.ShouldBeNull();
        fixture.FleetApplied.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_RequestAddAgvTypeConfiguration_InvalidSettings_ThrowsValidationException()
    {
        var fixture = new Fixture().Build();

        await MediatorShould.ThrowRequestExceptionAsync<ValidationException>(async () =>
            await fixture.GetResponseAsync<AgvTypeConfigurationResponse>(
                new RequestAddAgvTypeConfiguration(Guid.NewGuid(),
                    Fixture.AgvTypeIdentifier,
                    new AgvTypeSimulationSettings { NoOfAgvs = Fixture.NegativeNoOfAgvs })));

        fixture.AddedConfiguration.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_RequestUpdateAgvTypeConfiguration_ValidData_RespondsWithMergedConfiguration()
    {
        var storedConfiguration = Generator.Generate(noOfAgvs: Fixture.NoOfAgvs);
        var fixture = new Fixture().WithStoredConfiguration(storedConfiguration).Build();

        var response = await fixture.GetResponseAsync<RequestUpdateAgvTypeConfiguration, AgvTypeConfigurationResponse>(
            new RequestUpdateAgvTypeConfiguration(storedConfiguration.Id, new AgvTypeSimulationSettings { NoOfAgvs = Fixture.UpdatedNoOfAgvs }));

        response.Message.Configuration.NoOfAgvs.ShouldBe(Fixture.UpdatedNoOfAgvs);
        response.Message.Configuration.AgvTypeIdentifier.ShouldBe(Fixture.AgvTypeIdentifier);
        response.Message.Configuration.StateUpdateIntervalMs.ShouldBe(Generator.StateUpdateIntervalMs);
    }

    [TestMethod]
    public async Task Consume_RequestUpdateAgvTypeConfiguration_ValidData_StoresConfiguration()
    {
        var storedConfiguration = Generator.Generate(noOfAgvs: Fixture.NoOfAgvs);
        var fixture = new Fixture().WithStoredConfiguration(storedConfiguration).Build();

        await fixture.GetResponseAsync<RequestUpdateAgvTypeConfiguration, AgvTypeConfigurationResponse>(
            new RequestUpdateAgvTypeConfiguration(storedConfiguration.Id, new AgvTypeSimulationSettings { NoOfAgvs = Fixture.UpdatedNoOfAgvs }));

        fixture.UpdatedConfiguration.ShouldNotBeNull().NoOfAgvs.ShouldBe(Fixture.UpdatedNoOfAgvs);
    }

    [TestMethod]
    public async Task Consume_RequestUpdateAgvTypeConfiguration_ValidData_AppliesCurrentAndNewConfiguration()
    {
        var storedConfiguration = Generator.Generate(noOfAgvs: Fixture.NoOfAgvs);
        var fixture = new Fixture().WithStoredConfiguration(storedConfiguration).Build();

        await fixture.GetResponseAsync<RequestUpdateAgvTypeConfiguration, AgvTypeConfigurationResponse>(
            new RequestUpdateAgvTypeConfiguration(storedConfiguration.Id, new AgvTypeSimulationSettings { NoOfAgvs = Fixture.UpdatedNoOfAgvs }));

        fixture.AppliedCurrentConfiguration.ShouldNotBeNull().NoOfAgvs.ShouldBe(Fixture.NoOfAgvs);
        fixture.AppliedNewConfiguration.ShouldNotBeNull().NoOfAgvs.ShouldBe(Fixture.UpdatedNoOfAgvs);
    }

    [TestMethod]
    public async Task Consume_RequestUpdateAgvTypeConfiguration_UnknownAgvType_ThrowsAgvTypeConfigurationNotFoundException()
    {
        var fixture = new Fixture().Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvTypeConfigurationNotFoundException>(async () =>
            await fixture.GetResponseAsync<RequestUpdateAgvTypeConfiguration, AgvTypeConfigurationResponse>(
                new RequestUpdateAgvTypeConfiguration(Guid.NewGuid(), new AgvTypeSimulationSettings { NoOfAgvs = Fixture.UpdatedNoOfAgvs })));

        fixture.UpdatedConfiguration.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_RequestUpdateAgvTypeConfiguration_InvalidSettings_ThrowsValidationException()
    {
        var storedConfiguration = Generator.Generate(noOfAgvs: Fixture.NoOfAgvs);
        var fixture = new Fixture().WithStoredConfiguration(storedConfiguration).Build();

        await MediatorShould.ThrowRequestExceptionAsync<ValidationException>(async () =>
            await fixture.GetResponseAsync<RequestUpdateAgvTypeConfiguration, AgvTypeConfigurationResponse>(
                new RequestUpdateAgvTypeConfiguration(storedConfiguration.Id, new AgvTypeSimulationSettings { NoOfAgvs = Fixture.NegativeNoOfAgvs })));

        fixture.UpdatedConfiguration.ShouldBeNull();
        fixture.FleetApplied.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_RequestDeleteAgvTypeConfiguration_ValidData_DeletesConfiguration()
    {
        var storedConfiguration = Generator.Generate(noOfAgvs: Fixture.NoOfAgvs);
        var fixture = new Fixture().WithStoredConfiguration(storedConfiguration).Build();

        var response = await fixture.GetResponseAsync<RequestDeleteAgvTypeConfiguration, Success>(
            new RequestDeleteAgvTypeConfiguration(storedConfiguration.Id));

        response.Message.ShouldNotBeNull();
        fixture.DeletedAgvTypeId.ShouldBe(storedConfiguration.Id);
    }

    [TestMethod]
    public async Task Consume_RequestDeleteAgvTypeConfiguration_ValidData_RemovesAgvsOfAgvType()
    {
        var storedConfiguration = Generator.Generate(noOfAgvs: Fixture.NoOfAgvs);
        var fixture = new Fixture().WithStoredConfiguration(storedConfiguration).Build();

        await fixture.GetResponseAsync<RequestDeleteAgvTypeConfiguration, Success>(new RequestDeleteAgvTypeConfiguration(storedConfiguration.Id));

        fixture.AppliedCurrentConfiguration.ShouldNotBeNull().NoOfAgvs.ShouldBe(Fixture.NoOfAgvs);
        fixture.AppliedNewConfiguration.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_RequestDeleteAgvTypeConfiguration_UnknownAgvType_ThrowsAgvTypeConfigurationNotFoundException()
    {
        var fixture = new Fixture().Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvTypeConfigurationNotFoundException>(async () =>
            await fixture.GetResponseAsync<RequestDeleteAgvTypeConfiguration, Success>(new RequestDeleteAgvTypeConfiguration(Guid.NewGuid())));

        fixture.DeletedAgvTypeId.ShouldBeNull();
        fixture.FleetApplied.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_RequestAgvTypeConfiguration_KnownAgvType_RespondsWithConfiguration()
    {
        var storedConfiguration = Generator.Generate(noOfAgvs: Fixture.NoOfAgvs);
        var fixture = new Fixture().WithStoredConfiguration(storedConfiguration).Build();

        var response = await fixture.GetResponseAsync<RequestAgvTypeConfiguration, AgvTypeConfigurationResponse>(
            new RequestAgvTypeConfiguration(storedConfiguration.Id));

        response.Message.Configuration.AgvTypeIdentifier.ShouldBe(Fixture.AgvTypeIdentifier);
        response.Message.Configuration.NoOfAgvs.ShouldBe(Fixture.NoOfAgvs);
    }

    [TestMethod]
    public async Task Consume_RequestAgvTypeConfiguration_UnknownAgvType_ThrowsAgvTypeConfigurationNotFoundException()
    {
        var fixture = new Fixture().Build();

        await MediatorShould.ThrowRequestExceptionAsync<AgvTypeConfigurationNotFoundException>(async () =>
            await fixture.GetResponseAsync<RequestAgvTypeConfiguration, AgvTypeConfigurationResponse>(new RequestAgvTypeConfiguration(Guid.NewGuid())));
    }

    [TestMethod]
    public async Task Consume_RequestAgvTypeConfigurations_StoredConfigurations_RespondsWithAllConfigurations()
    {
        var fixture = new Fixture()
            .WithStoredConfiguration(Generator.Generate())
            .WithStoredConfiguration(Generator.Generate(Fixture.OtherAgvTypeIdentifier))
            .Build();

        var response = await fixture.GetResponseAsync<RequestAgvTypeConfigurations, AgvTypeConfigurationsResponse>(
            new RequestAgvTypeConfigurations(Guid.NewGuid()));

        response.Message.Configurations.Select(configuration => configuration.AgvTypeIdentifier)
            .ShouldBe(new[] { Fixture.AgvTypeIdentifier, Fixture.OtherAgvTypeIdentifier }, true);
    }

    [TestMethod]
    public async Task Consume_RequestAgvTypeConfigurations_NoStoredConfigurations_RespondsWithEmptyCollection()
    {
        var fixture = new Fixture().Build();

        var response = await fixture.GetResponseAsync<RequestAgvTypeConfigurations, AgvTypeConfigurationsResponse>(
            new RequestAgvTypeConfigurations(Guid.NewGuid()));

        response.Message.Configurations.ShouldBeEmpty();
    }
}