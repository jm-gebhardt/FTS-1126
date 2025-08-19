using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.DataAccess;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.Repositories;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

public abstract class ActionRepositoryConsumerTestFixture<TConsumer> : MqttConsumerTestFixture<TConsumer>
    where TConsumer : class, IConsumer
{
    private readonly DbTestFixtureBase<DeviceCommunicationDbContext> _dbFixture = new();

    public EfActionRepository ActionRepository { get; }

    protected ActionRepositoryConsumerTestFixture()
    {
        ActionRepository = new EfActionRepository(_dbFixture.ContextFactory.CreateDbContext());
        AddServices(s => s.AddTransient<IActionRepository>(_ => ActionRepository));
    }

    public async Task TryRemoveMappingAsync(Guid? internalOrderId, Guid vehicleOrderId)
    {
        if (internalOrderId != null)
        {
            await ActionRepository.RemoveVehicleAndInternalOrderIdMapping(vehicleOrderId);
        }
    }
}