using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using AgvStatus = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvStatus;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfAgvPositionRepositoryTest
{
    [TestMethod]
    public async Task RestorePersistedPositionsAsync_RestoresPositionsFromDb()
    {
        await using var fixture = new EfAgvPositionRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var position = new AgvPosition(true, 5, 10, 0, 1.5f);
        var status = new AgvStatus(correlationId, null, false, true, 100, false, false);
        status.UpdateLastKnownPosition(position);
        var agv = new Agv(correlationId, Guid.NewGuid(), "AGV001", status);
        await fixture.AddAsync(agv);

        await fixture.AgvPositionRepository.RestorePersistedPositionsAsync();

        var restored = fixture.AgvPositionRepository.GetPositionIfExisting(correlationId);
        restored.ShouldNotBeNull();
        restored.AgvPosition.X.ShouldBe(5);
        restored.AgvPosition.Y.ShouldBe(10);
        restored.AgvPosition.Theta.ShouldBe(1.5f);
        restored.SerialNumber.ShouldBe("AGV001");
    }

    [TestMethod]
    public async Task RestorePersistedPositionsAsync_DoesNotOverwriteExistingPositions()
    {
        await using var fixture = new EfAgvPositionRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var dbPosition = new AgvPosition(true, 5, 10, 0, 0);
        var status = new AgvStatus(correlationId, null, false, true, 100, false, false);
        status.UpdateLastKnownPosition(dbPosition);
        var agv = new Agv(correlationId, Guid.NewGuid(), "AGV001", status);
        await fixture.AddAsync(agv);

        var livePosition = new AgvPositionStatus(correlationId, "AGV001", new AgvPosition(true, 99, 99, 0, 0));
        fixture.AgvPositionDictionaryProvider.ConcurrentDict.TryAdd(correlationId, livePosition);

        await fixture.AgvPositionRepository.RestorePersistedPositionsAsync();

        var restored = fixture.AgvPositionRepository.GetPositionIfExisting(correlationId);
        restored.ShouldNotBeNull();
        restored.AgvPosition.X.ShouldBe(99);
        restored.AgvPosition.Y.ShouldBe(99);
    }

    [TestMethod]
    public async Task RestorePersistedPositionsAsync_SkipsAgvsWithoutLastKnownPosition()
    {
        await using var fixture = new EfAgvPositionRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var status = new AgvStatus(correlationId, null, false, true, 100, false, false);
        var agv = new Agv(correlationId, Guid.NewGuid(), "AGV001", status);
        await fixture.AddAsync(agv);

        await fixture.AgvPositionRepository.RestorePersistedPositionsAsync();

        var restored = fixture.AgvPositionRepository.GetPositionIfExisting(correlationId);
        restored.ShouldBeNull();
    }

    [TestMethod]
    public async Task RestorePersistedPositionsAsync_ClearsDbAfterRestore()
    {
        await using var fixture = new EfAgvPositionRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var position = new AgvPosition(true, 5, 10, 0, 0);
        var status = new AgvStatus(correlationId, null, false, true, 100, false, false);
        status.UpdateLastKnownPosition(position);
        var agv = new Agv(correlationId, Guid.NewGuid(), "AGV001", status);
        await fixture.AddAsync(agv);

        await fixture.AgvPositionRepository.RestorePersistedPositionsAsync();

        // Clear in-memory cache and try restoring again — DB should be empty
        fixture.AgvPositionDictionaryProvider.ConcurrentDict.Clear();
        await fixture.AgvPositionRepository.RestorePersistedPositionsAsync();

        var restored = fixture.AgvPositionRepository.GetPositionIfExisting(correlationId);
        restored.ShouldBeNull();
    }

    [TestMethod]
    public async Task PersistAllPositionsAsync_PersistsInMemoryPositionsToDb()
    {
        await using var fixture = new EfAgvPositionRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var status = new AgvStatus(correlationId, null, false, true, 100, false, false);
        var agv = new Agv(correlationId, Guid.NewGuid(), "AGV001", status);
        await fixture.AddAsync(agv);

        var position = new AgvPositionStatus(correlationId, "AGV001", new AgvPosition(true, 7, 14, 0, 2.0f));
        fixture.AgvPositionDictionaryProvider.ConcurrentDict.TryAdd(correlationId, position);

        await fixture.AgvPositionRepository.PersistAllPositionsAsync();

        // Verify DB has the position by reading directly
        await using var dbContext = await fixture.ContextFactory.CreateDbContextAsync();
        var dbAgv = await dbContext.Set<Agv>().Include(a => a.AgvStatus).FirstAsync(a => a.CorrelationId == correlationId);
        dbAgv.AgvStatus!.LastKnownPosition.ShouldNotBeNull();
        dbAgv.AgvStatus.LastKnownPosition!.X.ShouldBe(7);
        dbAgv.AgvStatus.LastKnownPosition.Y.ShouldBe(14);
        dbAgv.AgvStatus.LastKnownPosition.Theta.ShouldBe(2.0f);
    }

    [TestMethod]
    public async Task PersistPositionAsync_PersistsSinglePositionToDb()
    {
        await using var fixture = new EfAgvPositionRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var status = new AgvStatus(correlationId, null, false, true, 100, false, false);
        var agv = new Agv(correlationId, Guid.NewGuid(), "AGV001", status);
        await fixture.AddAsync(agv);

        var position = new AgvPosition(true, 3, 6, 0, 0.5f);
        await fixture.AgvPositionRepository.PersistPositionAsync(correlationId, position);

        await using var dbContext = await fixture.ContextFactory.CreateDbContextAsync();
        var dbAgv = await dbContext.Set<Agv>().Include(a => a.AgvStatus).FirstAsync(a => a.CorrelationId == correlationId);
        dbAgv.AgvStatus!.LastKnownPosition.ShouldNotBeNull();
        dbAgv.AgvStatus.LastKnownPosition!.X.ShouldBe(3);
        dbAgv.AgvStatus.LastKnownPosition.Y.ShouldBe(6);
        dbAgv.AgvStatus.LastKnownPosition.Theta.ShouldBe(0.5f);
    }

    [TestMethod]
    public async Task PersistPositionAsync_UnknownAgv_DoesNotThrow()
    {
        await using var fixture = new EfAgvPositionRepositoryTestFixture();

        await Should.NotThrowAsync(() => fixture.AgvPositionRepository.PersistPositionAsync(Guid.NewGuid(), new AgvPosition(true, 1, 1, 0, 0)));
    }

    [TestMethod]
    public async Task PersistAllPositionsAsync_PersistsMultipleAgvPositions()
    {
        await using var fixture = new EfAgvPositionRepositoryTestFixture();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var status1 = new AgvStatus(id1, null, false, true, 100, false, false);
        var status2 = new AgvStatus(id2, null, false, true, 100, false, false);
        await fixture.AddAsync(new Agv(id1, Guid.NewGuid(), "AGV001", status1));
        await fixture.AddAsync(new Agv(id2, Guid.NewGuid(), "AGV002", status2));

        fixture.AgvPositionDictionaryProvider.ConcurrentDict.TryAdd(id1, new AgvPositionStatus(id1, "AGV001", new AgvPosition(true, 1, 2, 0, 0)));
        fixture.AgvPositionDictionaryProvider.ConcurrentDict.TryAdd(id2, new AgvPositionStatus(id2, "AGV002", new AgvPosition(true, 3, 4, 0, 0)));

        await fixture.AgvPositionRepository.PersistAllPositionsAsync();

        await using var dbContext = await fixture.ContextFactory.CreateDbContextAsync();
        var agvs = await dbContext.Set<Agv>().Include(a => a.AgvStatus).OrderBy(a => a.SerialNumber).ToListAsync();
        agvs[0].AgvStatus!.LastKnownPosition.ShouldNotBeNull();
        agvs[0].AgvStatus!.LastKnownPosition!.X.ShouldBe(1);
        agvs[1].AgvStatus!.LastKnownPosition.ShouldNotBeNull();
        agvs[1].AgvStatus!.LastKnownPosition!.X.ShouldBe(3);
    }

    [TestMethod]
    public async Task ClearPersistedPositionAsync_ClearsPersistedPosition()
    {
        await using var fixture = new EfAgvPositionRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var status = new AgvStatus(correlationId, null, false, true, 100, false, false);
        status.UpdateLastKnownPosition(new AgvPosition(true, 5, 10, 0, 0));
        var agv = new Agv(correlationId, Guid.NewGuid(), "AGV001", status);
        await fixture.AddAsync(agv);

        await fixture.AgvPositionRepository.ClearPersistedPositionAsync(correlationId);

        await using var dbContext = await fixture.ContextFactory.CreateDbContextAsync();
        var dbAgv = await dbContext.Set<Agv>().Include(a => a.AgvStatus).FirstAsync(a => a.CorrelationId == correlationId);
        dbAgv.AgvStatus!.LastKnownPosition.ShouldBeNull();
    }

    [TestMethod]
    public async Task ClearPersistedPositionAsync_NoPosition_DoesNotThrow()
    {
        await using var fixture = new EfAgvPositionRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var status = new AgvStatus(correlationId, null, false, true, 100, false, false);
        var agv = new Agv(correlationId, Guid.NewGuid(), "AGV001", status);
        await fixture.AddAsync(agv);

        await Should.NotThrowAsync(() => fixture.AgvPositionRepository.ClearPersistedPositionAsync(correlationId));
    }

    [TestMethod]
    public async Task ClearPersistedPositionAsync_UnknownAgv_DoesNotThrow()
    {
        await using var fixture = new EfAgvPositionRepositoryTestFixture();

        await Should.NotThrowAsync(() => fixture.AgvPositionRepository.ClearPersistedPositionAsync(Guid.NewGuid()));
    }

    [TestMethod]
    public async Task PersistAllPositionsAsync_EmptyPositionList_DoesNotThrow()
    {
        await using var fixture = new EfAgvPositionRepositoryTestFixture();

        await Should.NotThrowAsync(() => fixture.AgvPositionRepository.PersistAllPositionsAsync());
    }

    [TestMethod]
    public async Task PersistAllPositionsAsync_AgvWithoutAgvStatus_SkipsGracefully()
    {
        await using var fixture = new EfAgvPositionRepositoryTestFixture();
        var correlationId = Guid.NewGuid();
        var agv = new Agv(correlationId, Guid.NewGuid(), "AGV001");
        await fixture.AddAsync(agv);

        var position = new AgvPositionStatus(correlationId, "AGV001", new AgvPosition(true, 7, 14, 0, 0));
        fixture.AgvPositionDictionaryProvider.ConcurrentDict.TryAdd(correlationId, position);

        await Should.NotThrowAsync(() => fixture.AgvPositionRepository.PersistAllPositionsAsync());
    }
}
