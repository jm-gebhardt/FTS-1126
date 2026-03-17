using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using MassTransit;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

internal class EfAgvPositionRepositoryTestFixture : TransportOrderDbFixture
{
    private readonly Mock<ISignificanceChecker<AgvPosition?>> _significanceChecker = new();
    private readonly Mock<IBus> _busMock = new();

    public AgvPositionRepository AgvPositionRepository { get; }

    public AgvPositionDictionaryProvider AgvPositionDictionaryProvider { get; } = new();

    public EfAgvPositionRepositoryTestFixture()
    {
        AgvPositionRepository = new AgvPositionRepository(_busMock.Object, AgvPositionDictionaryProvider, _significanceChecker.Object, ContextFactory);
    }
}
