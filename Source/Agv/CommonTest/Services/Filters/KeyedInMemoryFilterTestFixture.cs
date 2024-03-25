using Gebhardt.StoreWare.Agv.Common.Dictionaries;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Services.Filters;
using Gebhardt.StoreWare.Agv.Common.Testing.Filters;

namespace Gebhardt.StoreWare.Agv.CommonTest.Services.Filters;

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