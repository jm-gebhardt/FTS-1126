using Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Dtos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Hubs.Filters;

[TestClass]
public class InMemoryStorageFilterTests
{
    [TestMethod]
    public void ShouldPublish_WhenPreviousDataIsNull_ReturnsTrue()
    {
        var filter = new TestInMemoryStorageFilter(true);
        var message = new Message(DateTime.UtcNow);

        filter.ShouldPublish(message).ShouldBeTrue();
    }

    [TestMethod]
    public void ShouldPublish_WhenCurrentDataIsTheSameAsPreviousData_ReturnsFalse()
    {
        var filter = new TestInMemoryStorageFilter(false);
        var message = new Message(DateTime.UtcNow);
        filter.ShouldPublish(message);

        filter.ShouldPublish(message).ShouldBeFalse();
    }

    [TestMethod]
    public void ShouldPublish_WhenCurrentDataDiffersFromPreviousData_ReturnsTrue()
    {
        var filter = new TestInMemoryStorageFilter(true);
        var previousMessage = new Message(DateTime.UtcNow);
        filter.ShouldPublish(previousMessage);

        var newMessage = new Message(DateTime.UtcNow.AddSeconds(filter.MaxSilence.TotalSeconds));
        filter.ShouldPublish(newMessage).ShouldBeTrue();
    }

    [TestMethod]
    public void ShouldPublish_AfterMaxSilencePeriodIsExceeded_ReturnsTrue()
    {
        var filter = new TestInMemoryStorageFilter(false);
        var previousMessage = new Message(DateTime.UtcNow);
        filter.ShouldPublish(previousMessage);

        var newMessage = new Message(DateTime.UtcNow.AddSeconds(filter.MaxSilence.TotalSeconds));
        filter.ShouldPublish(newMessage).ShouldBeTrue();
    }

    [TestMethod]
    public void ShouldPublish_BeforeMaxSilencePeriodIsExceeded_ReturnsFalse()
    {
        var filter = new TestInMemoryStorageFilter(false);
        var previousMessage = new Message(DateTime.UtcNow);
        filter.ShouldPublish(previousMessage);

        var newMessage = new Message(DateTime.UtcNow.AddSeconds(filter.MaxSilence.TotalSeconds - 1));
        filter.ShouldPublish(newMessage).ShouldBeFalse();
    }

    [TestMethod]
    public void ShouldPublish_WhenMinSilenceIsZeroAndDataDiffers_ReturnsTrue()
    {
        var filter = new TestInMemoryStorageFilter(true, minSilence: TimeSpan.Zero, maxSilence: TimeSpan.FromSeconds(10));
        var previousMessage = new Message(DateTime.UtcNow);
        filter.ShouldPublish(previousMessage);

        var newMessage = new Message(DateTime.UtcNow);
        filter.ShouldPublish(newMessage).ShouldBeTrue();
    }

    [TestMethod]
    public void ShouldPublish_WhenMinSilenceIsNotMet_ReturnsFalse()
    {
        var filter = new TestInMemoryStorageFilter(false, minSilence: TimeSpan.FromSeconds(10), maxSilence: TimeSpan.FromSeconds(10));
        var previousMessage = new Message(DateTime.UtcNow);
        filter.ShouldPublish(previousMessage);

        var newMessage = new Message(DateTime.UtcNow);
        filter.ShouldPublish(newMessage).ShouldBeFalse();
    }

    [TestMethod]
    public void ShouldPublish_WhenMinSilenceIsMetAndDataDiffers_ReturnsTrue()
    {
        var filter = new TestInMemoryStorageFilter(true, minSilence: TimeSpan.FromSeconds(10), maxSilence: TimeSpan.FromSeconds(30));
        var previousMessage = new Message(DateTime.UtcNow);
        filter.ShouldPublish(previousMessage);

        var newMessage = new Message(previousMessage.MessageCreatedDate.AddSeconds(filter.MinSilence.TotalSeconds + 1));
        filter.ShouldPublish(newMessage).ShouldBeTrue();
    }

    [TestMethod]
    public void ShouldPublish_WhenPublishing_UpdatesPreviousData()
    {
        var filter = new TestInMemoryStorageFilter(true);
        var firstMessage = new Message(DateTime.UtcNow.AddMinutes(-1));
        filter.ShouldPublish(firstMessage).ShouldBeTrue();

        // Publish a second message that would only be true if the first was correctly set as previous
        var secondMessage = new Message(DateTime.UtcNow);
        filter.ShouldPublish(secondMessage).ShouldBeTrue();
    }
}