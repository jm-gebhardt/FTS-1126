using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class EfTriggerRepositoryTestFixture : TransportOrderDbFixture
{
    public readonly EfTriggerRepository Repository;

    public EfTriggerRepositoryTestFixture()
    {
        var mockLogger = new Mock<ILogger<EfTriggerRepository>>();
        var nodeTriggersDictionaryProvider = new NodeTriggersDictionaryProvider();
        Repository = new EfTriggerRepository(ContextFactory, nodeTriggersDictionaryProvider, mockLogger.Object);
    }
}