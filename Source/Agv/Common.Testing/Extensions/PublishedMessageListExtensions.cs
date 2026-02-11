using MassTransit.Testing;
using Shouldly;

namespace Gebhardt.Acc.Common.Testing.Extensions;

public static class PublishedMessageListExtensions
{
    extension(IPublishedMessageList published)
    {
        public async Task<T> GetFirstPublishedMessageAsync<T>(int occurrenceNumber = 1) where T : class
        {
            var timeout = TimeSpan.FromSeconds(10);
            var startTime = DateTime.UtcNow;
            
            while (DateTime.UtcNow - startTime < timeout)
            {
                List<IPublishedMessage<T>> messages = published.Select<T>().ToList();
                if (messages.Count >= occurrenceNumber)
                {
                    return messages[occurrenceNumber - 1].Context.Message;
                }
                await Task.Delay(50);
            }
            
            throw new TimeoutException($"Timed out waiting for {occurrenceNumber} message(s) of type {typeof(T).Name}. Only {published.Select<T>().Count()} message(s) available.");
        }

        public async Task ShouldHavePublishedAsync<T>() where T : class
        {
            (await published.Any<T>()).ShouldBeTrue();
        }

        public async Task ShouldNotHavePublishedAsync<T>() where T : class
        {
            (await published.Any<T>()).ShouldBeFalse();
        }

        public async Task ShouldHavePublishedCountAsync<T>(int expectedCount) where T : class
        {
            await AsyncElementListExtensions.Take(published.SelectAsync<T>(), expectedCount).ToListAsync();
            published.Select<T>().Count().ShouldBe(expectedCount);
        }

        public ICollection<T> GetPublishedMessages<T>() where T : class
        {
            return published.Select<T>().Select(m => m.Context.Message).ToList();
        }
    }
}