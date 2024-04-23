using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Testing.Filters;

namespace Gebhardt.Acc.CommonTest.Services.Filters;

internal class KeyedInMemoryFilterTestFixture
{
    public TestKeyedInMemoryFilter Filter { get; }

    public IConcurrentDictionaryProvider<Guid, IMessageFilter<Message>> ConcurrentDictionaryProvider { get; } = new ConcurrentDictionaryProvider<Guid, IMessageFilter<Message>>();

    public KeyedInMemoryFilterTestFixture()
    {
        Filter = new TestKeyedInMemoryFilter(new SemaphoreProvider(),
            ConcurrentDictionaryProvider,
            new FilterMock<Message>(true)
        );
    }

    public KeyedInMemoryFilterTestFixture WithDictionaryItem(Guid key, IMessageFilter<Message> value)
    {
        ConcurrentDictionaryProvider.Add(key, value);
        return this;
    }
}