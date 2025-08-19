using MassTransit.Testing;
using Shouldly;

namespace Gebhardt.Acc.Common.Testing.Extensions;

public static class PublishedMessageListExtensions
{
    public static async Task<T> GetFirstPublishedMessageAsync<T>(this IPublishedMessageList published, int occurrenceNumber = 1) where T : class
    {
        await published.ShouldHavePublishedAsync<T>();
        IPublishedMessage<T>? first = published.Select<T>()
            .Skip(occurrenceNumber - 1)
            .First();
        return first.Context.Message;
    }

    public static async Task ShouldHavePublishedAsync<T>(this IPublishedMessageList published) where T : class
    {
        (await published.Any<T>()).ShouldBeTrue();
    }

    public static async Task ShouldNotHavePublishedAsync<T>(this IPublishedMessageList published) where T : class
    {
        (await published.Any<T>()).ShouldBeFalse();
    }

    public static ICollection<T> GetPublishedMessages<T>(this IPublishedMessageList published) where T : class
    {
        return published.Select<T>().Select(m => m.Context.Message).ToList();
    }
}