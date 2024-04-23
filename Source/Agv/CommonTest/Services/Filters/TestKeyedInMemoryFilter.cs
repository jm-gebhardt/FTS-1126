using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;

namespace Gebhardt.Acc.CommonTest.Services.Filters;

internal class TestKeyedInMemoryFilter(
    ISemaphoreProvider semaphoreProvider,
    IConcurrentDictionaryProvider<Guid, IMessageFilter<Message>> concurrentDictionaryProvider,
    IMessageFilter<Message> filter)
    : KeyedInMemoryFilter<Message>(semaphoreProvider, concurrentDictionaryProvider)
{
    protected override Guid GetKey(Message value) => value.Key;

    protected override IMessageFilter<Message> GetFilter(Message value) => filter;
}