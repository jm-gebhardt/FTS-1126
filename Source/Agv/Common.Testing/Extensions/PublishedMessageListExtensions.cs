using MassTransit.Testing;
using Shouldly;

namespace Gebhardt.Acc.Common.Testing.Extensions;

public static class PublishedMessageListExtensions
{
    extension(IPublishedMessageList published)
    {
        public async Task<T> GetFirstPublishedMessageAsync<T>(int occurrenceNumber = 1) where T : class
        {
            await published.ShouldHavePublishedAsync<T>();
            IPublishedMessage<T>? first = published.Select<T>()
                .Skip(occurrenceNumber - 1)
                .First();
            return first.Context.Message;
        }

        public async Task ShouldHavePublishedAsync<T>() where T : class
        {
            (await published.Any<T>()).ShouldBeTrue();
        }

        public async Task ShouldNotHavePublishedAsync<T>() where T : class
        {
            (await published.Any<T>()).ShouldBeFalse();
        }

        public ICollection<T> GetPublishedMessages<T>() where T : class
        {
            return published.Select<T>().Select(m => m.Context.Message).ToList();
        }
    }
}