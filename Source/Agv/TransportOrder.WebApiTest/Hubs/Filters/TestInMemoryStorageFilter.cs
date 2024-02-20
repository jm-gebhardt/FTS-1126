using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Dtos;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs.Filters;

internal class TestInMemoryStorageFilter : InMemoryStorageFilter<Message>
{
    private readonly bool _shouldPublishBasedOnReturnValue;

    public override TimeSpan MinSilence { get; }

    public override TimeSpan MaxSilence { get; }

    public TestInMemoryStorageFilter(bool shouldPublishBasedOnReturnValue)
    {
        _shouldPublishBasedOnReturnValue = shouldPublishBasedOnReturnValue;
    }

    internal TestInMemoryStorageFilter(bool shouldPublishBasedOnReturnValue, TimeSpan minSilence, TimeSpan maxSilence)
    {
        _shouldPublishBasedOnReturnValue = shouldPublishBasedOnReturnValue;
        MinSilence = minSilence;
        MaxSilence = maxSilence;
    }

    protected override bool ShouldPublishBasedOn(Message currentData, Message previousData)
    {
        return _shouldPublishBasedOnReturnValue;
    }
}