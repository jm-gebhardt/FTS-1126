using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Repositories;

public class AgvErrorRepositoryTestFixture : TransportOrderDbFixture
{
    public EfAgvErrorRepository AgvErrorRepository { get; }

    public AgvErrorRepositoryTestFixture()
    {
        AgvErrorRepository = new EfAgvErrorRepository(ContextFactory);
    }

    public AgvErrorRepositoryTestFixture WithAgvErrorStatus(AgvErrorStatusReceived agvErrorStatus)
    {
        AddInitTask(AgvErrorRepository.AddOrUpdateAgvErrorStatusAsync(agvErrorStatus.Errors, agvErrorStatus.CorrelationId));
        return this;
    }

    private new void Build()
    {
        base.Build();
    }

    public new async Task<AgvErrorRepositoryTestFixture> BuildAsync()
    {
        await base.BuildAsync();
        Build();
        return this;
    }
}