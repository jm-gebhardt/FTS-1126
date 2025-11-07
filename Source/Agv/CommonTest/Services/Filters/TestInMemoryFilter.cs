using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Services.Filters.Configurations;

namespace Gebhardt.Acc.CommonTest.Services.Filters;

internal class TestInMemoryFilter(bool shouldPublishBasedOnReturnValue) : InMemoryFilter<Message>(new SemaphoreProvider(), new TestFilterConfiguration())
{
    public override TimeSpan MinInterval { get; } = TimeSpan.MinValue;

    public override TimeSpan MaxInterval { get; } = TimeSpan.MaxValue;

    internal TestInMemoryFilter(bool shouldPublishBasedOnReturnValue, TimeSpan minSilence, TimeSpan maxSilence) : this(shouldPublishBasedOnReturnValue)
    {
        MinInterval = minSilence;
        MaxInterval = maxSilence;
    }

    protected override bool ShouldActOnData(Message currentData, Message previousData)
    {
        return shouldPublishBasedOnReturnValue;
    }
}

internal record TestFilterConfiguration : FilterConfiguration;