using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Services.Filters;

namespace Gebhardt.StoreWare.Agv.CommonTest.Services.Filters;

internal class TestInMemoryFilter(bool shouldPublishBasedOnReturnValue) : InMemoryFilter<Message>(new SemaphoreProvider())
{
    public override TimeSpan MinInterval { get; }

    public override TimeSpan MaxInterval { get; }

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