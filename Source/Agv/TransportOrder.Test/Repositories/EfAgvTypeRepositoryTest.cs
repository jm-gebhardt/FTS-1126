using Gebhardt.Acc.Common.Domain.Utilities;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfAgvTypeRepositoryTest
{
    [TestMethod]
    public async Task ExistsAsync_KnownIdentifier_ReturnsTrue()
    {
        var fixture = new AgvTypeRepositoryTestFixture();
        await fixture.Repository.AddOrUpdateAsync(AgvTypeRepositoryTestFixture.GenerateAgvType(), CancellationToken.None);

        bool exists = await fixture.Repository.ExistsAsync(AgvTypeRepositoryTestFixture.AgvTypeIdentifier);

        exists.ShouldBeTrue();
    }

    [TestMethod]
    public async Task ExistsAsync_UnknownIdentifier_ReturnsFalse()
    {
        var fixture = new AgvTypeRepositoryTestFixture();

        bool exists = await fixture.Repository.ExistsAsync(AgvTypeRepositoryTestFixture.AgvTypeIdentifier);

        exists.ShouldBeFalse();
    }

    [TestMethod]
    public async Task AddOrUpdateAsync_NewType_Persists()
    {
        var fixture = new AgvTypeRepositoryTestFixture();
        var agvType = AgvTypeRepositoryTestFixture.GenerateAgvType();

        await fixture.Repository.AddOrUpdateAsync(agvType, CancellationToken.None);

        var persisted = await fixture.Repository.GetAgvTypeAsync(AgvTypeRepositoryTestFixture.AgvTypeIdentifier.ToGuid(), CancellationToken.None);
        persisted.ShouldBe(agvType);
    }

    [TestMethod]
    public async Task AddOrUpdateAsync_ExistingType_UpdatesInPlace()
    {
        var fixture = new AgvTypeRepositoryTestFixture();
        await fixture.Repository.AddOrUpdateAsync(AgvTypeRepositoryTestFixture.GenerateAgvType(), CancellationToken.None);

        await fixture.Repository.AddOrUpdateAsync(
            AgvTypeRepositoryTestFixture.GenerateAgvType(AgvTypeRepositoryTestFixture.UpdatedLength, AgvTypeRepositoryTestFixture.UpdatedWidth, acknowledged: true),
            CancellationToken.None);

        var persisted = await fixture.GetAsync(AgvTypeRepositoryTestFixture.AgvTypeIdentifier);
        persisted.ShouldNotBeNull();
        persisted.Length.ShouldBe(AgvTypeRepositoryTestFixture.UpdatedLength);
        persisted.Width.ShouldBe(AgvTypeRepositoryTestFixture.UpdatedWidth);
        persisted.Acknowledged.ShouldBeTrue();
    }

    [TestMethod]
    public async Task AddOrUpdateAsync_NewType_PersistsReportedByAgv()
    {
        var fixture = new AgvTypeRepositoryTestFixture();

        await fixture.Repository.AddOrUpdateAsync(AgvTypeRepositoryTestFixture.GenerateAgvType(reportedByAgv: true));

        var persisted = await fixture.GetAsync(AgvTypeRepositoryTestFixture.AgvTypeIdentifier);
        persisted.ShouldNotBeNull();
        persisted.ReportedByAgv.ShouldBeTrue();
    }

    [TestMethod]
    public async Task AddOrUpdateAsync_AcknowledgedReportedType_KeepsReportedByAgv()
    {
        var fixture = new AgvTypeRepositoryTestFixture();
        await fixture.Repository.AddOrUpdateAsync(AgvTypeRepositoryTestFixture.GenerateAgvType(reportedByAgv: true));
        var stored = await fixture.Repository.GetAgvTypeAsync(AgvTypeRepositoryTestFixture.AgvTypeIdentifier.ToGuid());
        stored.Acknowledge();

        await fixture.Repository.AddOrUpdateAsync(stored);

        var persisted = await fixture.GetAsync(AgvTypeRepositoryTestFixture.AgvTypeIdentifier);
        persisted.ShouldNotBeNull();
        persisted.Acknowledged.ShouldBeTrue();
        persisted.ReportedByAgv.ShouldBeTrue();
    }
}