using Gebhardt.StoreWare.Agv.Common.Dictionaries;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Services.Filters;

namespace Gebhardt.StoreWare.Agv.CommonTest.Services.Filters;

internal class TestKeyedInMemoryFilter(
    ISemaphoreProvider semaphoreProvider,
    IConcurrentDictionaryProvider<Guid, IMessageFilter<Message>> concurrentDictionaryProvider,
    IMessageFilter<Message> filter)
    : KeyedInMemoryFilter<Message>(semaphoreProvider, concurrentDictionaryProvider)
{
    protected override Guid GetKey(Message value) => value.Key;

    protected override IMessageFilter<Message> GetFilter(Message value) => filter;
}