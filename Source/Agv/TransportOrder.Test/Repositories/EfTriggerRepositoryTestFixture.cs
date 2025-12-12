using AutoMapper;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfTriggerRepositoryTestFixture : NodeTestFixture
{
    public readonly EfTriggerRepository Repository;

    protected override IEnumerable<Profile> GetProfiles()
    {
        return [new GraphProfile()];
    }

    public EfTriggerRepositoryTestFixture()
    {
        var mockLogger = new Mock<ILogger<EfTriggerRepository>>();
        var triggerLocationDictionaryProvider = new TriggerLocationDictionaryProvider();
        Repository = new EfTriggerRepository(ContextFactory, triggerLocationDictionaryProvider, mockLogger.Object);
        Build();
    }

    protected sealed override void Build()
    {
        base.Build();
    }

    public Task AddTriggerAsync(Trigger trigger)
    {
        return Repository.AddOrUpdate(trigger, true);
    }
}