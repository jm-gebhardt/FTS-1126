using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Services.Filters;

[TestClass]
public class InMemoryFilterTests
{
    [TestMethod]
    public async Task ShouldPublish_WhenPreviousDataIsNull_ReturnsTrue()
    {
        var filter = new TestInMemoryFilter(true);
        var message = new Message(DateTime.UtcNow);

        (await filter.ShouldActAsync(message)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task ShouldPublish_WhenCurrentDataIsTheSameAsPreviousData_ReturnsFalse()
    {
        var filter = new TestInMemoryFilter(false);
        var message = new Message(DateTime.UtcNow);
        await filter.ShouldActAsync(message);

        (await filter.ShouldActAsync(message)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task ShouldPublish_AfterMaxSilencePeriodIsExceeded_ReturnsTrue()
    {
        var filter = new TestInMemoryFilter(false);
        var previousMessage = new Message(DateTime.UtcNow);
        await filter.ShouldActAsync(previousMessage);

        var newMessage = new Message(DateTime.UtcNow.AddSeconds(filter.MaxInterval.TotalSeconds));
        (await filter.ShouldActAsync(newMessage)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task ShouldPublish_BeforeMaxSilencePeriodIsExceeded_ReturnsFalse()
    {
        var filter = new TestInMemoryFilter(false);
        var previousMessage = new Message(DateTime.UtcNow);
        await filter.ShouldActAsync(previousMessage);

        var newMessage = new Message(DateTime.UtcNow.AddSeconds(filter.MaxInterval.TotalSeconds - 1));
        (await filter.ShouldActAsync(newMessage)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task ShouldPublish_WhenMinSilenceIsZeroAndDataDiffers_ReturnsTrue()
    {
        var filter = new TestInMemoryFilter(true, minSilence: TimeSpan.Zero, maxSilence: TimeSpan.FromSeconds(10));
        var previousMessage = new Message(DateTime.UtcNow);
        await filter.ShouldActAsync(previousMessage);

        var newMessage = new Message(DateTime.UtcNow);
        (await filter.ShouldActAsync(newMessage)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task ShouldPublish_WhenMinSilenceIsNotMet_ReturnsFalse()
    {
        var filter = new TestInMemoryFilter(false, minSilence: TimeSpan.FromSeconds(10), maxSilence: TimeSpan.FromSeconds(10));
        var previousMessage = new Message(DateTime.UtcNow);
        await filter.ShouldActAsync(previousMessage);

        var newMessage = new Message(DateTime.UtcNow);
        (await filter.ShouldActAsync(newMessage)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task ShouldPublish_WhenMinSilenceIsMetAndDataDiffers_ReturnsTrue()
    {
        var filter = new TestInMemoryFilter(true, minSilence: TimeSpan.FromSeconds(10), maxSilence: TimeSpan.FromSeconds(30));
        var previousMessage = new Message(DateTime.UtcNow);
        await filter.ShouldActAsync(previousMessage);

        var newMessage = new Message(previousMessage.MessageCreatedDate.AddSeconds(filter.MinInterval.TotalSeconds + 1));
        (await filter.ShouldActAsync(newMessage)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task ShouldPublish_WhenPublishing_UpdatesPreviousData()
    {
        var filter = new TestInMemoryFilter(true);
        var firstMessage = new Message(DateTime.UtcNow.AddMinutes(-1));
        (await filter.ShouldActAsync(firstMessage)).ShouldBeTrue();

        // Publish a second message that would only be true if the first was correctly set as previous
        var secondMessage = new Message(DateTime.UtcNow);
        (await filter.ShouldActAsync(secondMessage)).ShouldBeTrue();
    }
}