using Gebhardt.Acc.Common.Memory;
using Gebhardt.Acc.Statistics.Cache;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.Statistics.Test.Cache;

public class AgvScopedPositionCacheFixture
{
    public AgvScopedPositionCache<Guid> Cache { get; }

    public AgvScopedPositionCacheFixture()
    {
        var innerCache = new Cache<string, List<Guid>>();
        var loggerMock = new Mock<ILogger<AgvScopedPositionCache<Guid>>>();
        Cache = new AgvScopedPositionCache<Guid>(
            innerCache,
            maxItemsPerAgv: 10,
            logger: loggerMock.Object,
            warnOnOverflow: false
        );
    }
}