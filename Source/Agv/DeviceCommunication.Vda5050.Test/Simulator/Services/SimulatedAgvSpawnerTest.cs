using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Shouldly;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services.SimulatedAgvSpawnerTestFixture;
using Generator = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Simulator.SimulatedAgvTypeConfigurationGenerator;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

[TestClass]
public class SimulatedAgvSpawnerTest
{
    [TestMethod]
    public async Task ApplyAsync_NoCurrentConfiguration_PublishesCreateAgvPerSerialNumber()
    {
        var fixture = new Fixture().Build();

        await fixture.Sut.ApplyAsync(null, Generator.Generate(noOfAgvs: Fixture.NoOfAgvs));

        fixture.GetPublishedMessages<CreateAgv>()
            .Select(c => c.Configuration.SerialNumber)
            .ShouldBe([Fixture.FirstSerialNumber, Fixture.SecondSerialNumber], true);
        fixture.GetPublishedMessages<UpdateAgvConfiguration>().ShouldBeEmpty();
        fixture.GetPublishedMessages<RemoveAgv>().ShouldBeEmpty();
    }

    [TestMethod]
    public async Task ApplyAsync_NoCurrentConfiguration_CreateAgvCarriesConfiguration()
    {
        var fixture = new Fixture().Build();

        await fixture.Sut.ApplyAsync(null, Generator.Generate(noOfAgvs: Fixture.SingleAgv));

        var created = fixture.GetPublishedMessages<CreateAgv>().ShouldHaveSingleItem();
        created.Configuration.SerialNumber.ShouldBe(Fixture.FirstSerialNumber);
        created.Configuration.Manufacturer.ShouldBe(Fixture.Manufacturer);
        created.Configuration.MapId.ShouldBe(Fixture.MapId);
        created.Configuration.AgvTypeId.ShouldBe(Fixture.AgvTypeIdentifier.ToGuid());
        created.Configuration.StateUpdateIntervalMs.ShouldBe(Generator.StateUpdateIntervalMs);
        created.Configuration.VisualizationUpdateIntervalMs.ShouldBe(Generator.VisualizationUpdateIntervalMs);
        created.Configuration.BatteryDischargeRate.ShouldBe(Generator.BatteryDischargeRate);
        created.Configuration.BatteryChargeRate.ShouldBe(Generator.BatteryChargeRate);
        created.Configuration.InitialBatteryCharge.ShouldBe(Generator.InitialBatteryCharge);
    }

    [TestMethod]
    public async Task ApplyAsync_EmptyGraph_CreateAgvWithoutInitialGraphNodes()
    {
        var fixture = new Fixture().Build();

        await fixture.Sut.ApplyAsync(null, Generator.Generate(noOfAgvs: Fixture.SingleAgv));

        fixture.GetPublishedMessages<CreateAgv>().ShouldHaveSingleItem().InitialNodes.ShouldBeNull();
    }

    [TestMethod]
    public async Task ApplyAsync_ExistingGraph_CreateAgvCarriesInitialGraphNodes()
    {
        var fixture = new Fixture().WithGraphNode().Build();

        await fixture.Sut.ApplyAsync(null, Generator.Generate(noOfAgvs: Fixture.SingleAgv));

        fixture.GetPublishedMessages<CreateAgv>()
            .ShouldHaveSingleItem()
            .InitialNodes
            .ShouldNotBeNull()
            .ShouldHaveSingleItem();
    }

    [TestMethod]
    public async Task ApplyAsync_MoreAgvs_PublishesCreateAgvForAddedSerialNumbersOnly()
    {
        var fixture = new Fixture().Build();

        await fixture.Sut.ApplyAsync(Generator.Generate(noOfAgvs: Fixture.NoOfAgvs), Generator.Generate(noOfAgvs: Fixture.MoreAgvs));

        fixture.GetPublishedMessages<CreateAgv>()
            .Select(c => c.Configuration.SerialNumber)
            .ShouldBe([Fixture.ThirdSerialNumber, Fixture.FourthSerialNumber], true);
        fixture.GetPublishedMessages<UpdateAgvConfiguration>().ShouldBeEmpty();
        fixture.GetPublishedMessages<RemoveAgv>().ShouldBeEmpty();
    }

    [TestMethod]
    public async Task ApplyAsync_FewerAgvs_PublishesRemoveAgvForHighestSerialNumbersOnly()
    {
        var fixture = new Fixture().Build();

        await fixture.Sut.ApplyAsync(Generator.Generate(noOfAgvs: Fixture.MoreAgvs), Generator.Generate(noOfAgvs: Fixture.NoOfAgvs));

        fixture.GetPublishedMessages<RemoveAgv>()
            .Select(r => r.CorrelationId)
            .ShouldBe([Fixture.ThirdSerialNumber.ToGuid(), Fixture.FourthSerialNumber.ToGuid()], true);
        fixture.GetPublishedMessages<CreateAgv>().ShouldBeEmpty();
        fixture.GetPublishedMessages<UpdateAgvConfiguration>().ShouldBeEmpty();
    }

    [TestMethod]
    public async Task ApplyAsync_ChangedSettings_PublishesUpdateAgvConfigurationPerSerialNumber()
    {
        var fixture = new Fixture().Build();

        await fixture.Sut.ApplyAsync(Generator.Generate(noOfAgvs: Fixture.NoOfAgvs),
            Generator.Generate(noOfAgvs: Fixture.NoOfAgvs, stateUpdateIntervalMs: Fixture.ChangedStateUpdateIntervalMs));

        ICollection<UpdateAgvConfiguration> updated = fixture.GetPublishedMessages<UpdateAgvConfiguration>();
        updated.Select(u => u.Configuration.SerialNumber)
            .ShouldBe([Fixture.FirstSerialNumber, Fixture.SecondSerialNumber], true);
        updated.ShouldAllBe(u => u.Configuration.StateUpdateIntervalMs == Fixture.ChangedStateUpdateIntervalMs);
        fixture.GetPublishedMessages<CreateAgv>().ShouldBeEmpty();
        fixture.GetPublishedMessages<RemoveAgv>().ShouldBeEmpty();
    }

    [TestMethod]
    public async Task ApplyAsync_UnchangedConfiguration_NothingPublished()
    {
        var fixture = new Fixture().Build();
        var configuration = Generator.Generate(noOfAgvs: Fixture.NoOfAgvs);

        await fixture.Sut.ApplyAsync(configuration, configuration);

        fixture.GetPublishedMessages<CreateAgv>().ShouldBeEmpty();
        fixture.GetPublishedMessages<UpdateAgvConfiguration>().ShouldBeEmpty();
        fixture.GetPublishedMessages<RemoveAgv>().ShouldBeEmpty();
    }

    [TestMethod]
    public async Task ApplyAsync_NoNewConfiguration_PublishesRemoveAgvPerSerialNumber()
    {
        var fixture = new Fixture().Build();

        await fixture.Sut.ApplyAsync(Generator.Generate(noOfAgvs: Fixture.NoOfAgvs), null);

        fixture.GetPublishedMessages<RemoveAgv>()
            .Select(r => r.CorrelationId)
            .ShouldBe([Fixture.FirstSerialNumber.ToGuid(), Fixture.SecondSerialNumber.ToGuid()], true);
        fixture.GetPublishedMessages<CreateAgv>().ShouldBeEmpty();
        fixture.GetPublishedMessages<UpdateAgvConfiguration>().ShouldBeEmpty();
    }

    [TestMethod]
    public async Task ApplyAsync_MoreAgvsAndChangedSettings_AddedAgvsCreatedAndExistingAgvsUpdated()
    {
        var fixture = new Fixture().Build();

        await fixture.Sut.ApplyAsync(Generator.Generate(noOfAgvs: Fixture.NoOfAgvs),
            Generator.Generate(noOfAgvs: Fixture.MoreAgvs, stateUpdateIntervalMs: Fixture.ChangedStateUpdateIntervalMs));

        fixture.GetPublishedMessages<CreateAgv>()
            .Select(c => c.Configuration.SerialNumber)
            .ShouldBe([Fixture.ThirdSerialNumber, Fixture.FourthSerialNumber], true);
        fixture.GetPublishedMessages<UpdateAgvConfiguration>()
            .Select(u => u.Configuration.SerialNumber)
            .ShouldBe([Fixture.FirstSerialNumber, Fixture.SecondSerialNumber], true);
        fixture.GetPublishedMessages<RemoveAgv>().ShouldBeEmpty();
    }
}