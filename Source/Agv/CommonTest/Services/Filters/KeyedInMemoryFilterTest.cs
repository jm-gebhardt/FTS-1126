using Gebhardt.Acc.Common.Testing.Filters;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Services.Filters;

[TestClass]
public class KeyedInMemoryFilterTest
{
    [TestMethod]
    public async Task ShouldAct_WhenDictionaryEmpty_ReturnsTrue()
    {
        var fixture = new KeyedInMemoryFilterTestFixture();
        var testObject = new Message();
        (await fixture.Filter.ShouldActAsync(testObject)).ShouldBeTrue();
        fixture.ConcurrentDictionaryProvider.ConcurrentDict.Count.ShouldBe(1);
        var filter = fixture.ConcurrentDictionaryProvider.ConcurrentDict.First();
        filter.Key.ShouldBe(testObject.Key);
    }

    [TestMethod]
    public async Task ShouldAct_WhenDictionaryContainsFilterReturningTrue_ReturnsTrue()
    {
        var filterMock = new FilterMock<Message>()
            .Returns(true);
        var testObject = new Message();
        var fixture = new KeyedInMemoryFilterTestFixture()
            .WithDictionaryItem(testObject.Key, filterMock);
        (await fixture.Filter.ShouldActAsync(testObject)).ShouldBeTrue();
        fixture.ConcurrentDictionaryProvider.ConcurrentDict.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task ShouldAct_WhenDictionaryContainsFilterReturningFalse_ReturnsFalse()
    {
        var filterMock = new FilterMock<Message>()
            .Returns(false);
        var testObject = new Message();
        var fixture = new KeyedInMemoryFilterTestFixture()
            .WithDictionaryItem(testObject.Key, filterMock);
        (await fixture.Filter.ShouldActAsync(testObject)).ShouldBeFalse();
        fixture.ConcurrentDictionaryProvider.ConcurrentDict.Count.ShouldBe(1);
    }
}