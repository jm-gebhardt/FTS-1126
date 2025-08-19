using MassTransit.Testing;
using Shouldly;

namespace Gebhardt.Acc.Common.Testing.Extensions;

public static class ReceivedMessageListExtensions
{
    public static async Task<T> GetFirstConsumedMessageAsync<T>(this IReceivedMessageList received) where T : class
    {
        IReceivedMessage<T> first = await received.SelectAsync<T>().First();
        return first.Context.Message;
    }

    public static async Task ShouldHaveConsumedAsync<T>(this IReceivedMessageList received) where T : class
    {
        (await received.Any<T>()).ShouldBeTrue();
    }

    public static async Task ShouldNotHaveConsumedAsync<T>(this IReceivedMessageList received) where T : class
    {
        (await received.Any<T>()).ShouldBeFalse();
    }
}