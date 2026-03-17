using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

internal class AgvPositionRepositoryTestFixture
{
    private readonly Mock<ISignificanceChecker<AgvPosition?>> _significanceChecker = new();
    private readonly Mock<IDbContextFactory<TransportOrderDbContext>> _dbContextFactory = new();
    public readonly Mock<IBus> BusMock = new();

    public AgvPositionRepository AgvPositionRepository { get; }

    public AgvPositionDictionaryProvider AgvPositionDictionaryProvider { get; } = new();

    public AgvPositionRepositoryTestFixture()
    {
        AgvPositionRepository = new AgvPositionRepository(BusMock.Object, AgvPositionDictionaryProvider, _significanceChecker.Object, _dbContextFactory.Object);
    }

    public AgvPositionRepositoryTestFixture WithSignificanceCheckerReturns(bool returnValue)
    {
        _significanceChecker.Setup(s => s.HasSignificantChanges(It.IsAny<AgvPosition?>(), It.IsAny<AgvPosition?>())).Returns(returnValue);
        return this;
    }

    public AgvPositionRepositoryTestFixture WithAgvPositionStatus(AgvPositionStatus agvPositionStatus)
    {
        AgvPositionDictionaryProvider.ConcurrentDict.AddOrUpdate(agvPositionStatus.CorrelationId, agvPositionStatus, (_, _) => agvPositionStatus);
        return this;
    }
}