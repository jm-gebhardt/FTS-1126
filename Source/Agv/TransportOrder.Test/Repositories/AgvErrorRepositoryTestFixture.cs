using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Domain.Dictionaries;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class AgvErrorRepositoryTestFixture : TransportOrderDbFixture
{
    public EfAgvErrorRepository AgvErrorRepository { get; }

    private readonly IConcurrentDictionaryProvider<Guid, Lazy<List<AgvError>>> _errorCache = new AgvErrorDictionaryProvider();

    public AgvErrorRepositoryTestFixture()
    {
        AgvErrorRepository = new EfAgvErrorRepository(ContextFactory, _errorCache, Mock.Of<ILogger<EfAgvErrorRepository>>());
    }

    public AgvErrorRepositoryTestFixture WithAgvErrors(List<AgvError> errors)
    {
        AddInitTask(AgvErrorRepository.AddOrUpdateAgvErrorStatus(errors, errors.GroupBy(e => e.AgvCorrelationId).Single().Key));
        return this;
    }

    public AgvErrorRepositoryTestFixture WithAgvErrorStatus(AgvErrorStatusReceived agvErrorStatus) => WithAgvErrors(agvErrorStatus.Errors);

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

    public List<AgvError> GetCachedErrors(Guid agvCorrelationId)
    {
        _errorCache.ConcurrentDict.TryGetValue(agvCorrelationId, out Lazy<List<AgvError>>? errors);
        return errors?.Value ?? [];
    }
}