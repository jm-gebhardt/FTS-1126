using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;
using Gebhardt.Acc.TransportOrder.Test.Repositories;

namespace Gebhardt.Acc.KaeserSidecar.Test.Domain.Repositories;

public class TransportOrderStationRepositoryTestFixture: TransportOrderDbFixture
{
    public readonly ITransportOrderStationRepository Repository;

    public TransportOrderStationRepositoryTestFixture()
    {
        Repository = new TransportOrderStationRepository(ContextFactory);
    }
}