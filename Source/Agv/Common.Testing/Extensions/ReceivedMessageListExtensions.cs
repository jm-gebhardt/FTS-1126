using MassTransit.Testing;
using Shouldly;

namespace Gebhardt.Acc.Common.Testing.Extensions;

public static class ReceivedMessageListExtensions
{
    extension(IReceivedMessageList received)
    {
        public async Task<T> GetFirstConsumedMessageAsync<T>() where T : class
        {
            IReceivedMessage<T> first = await received.SelectAsync<T>().First();
            return first.Context.Message;
        }

        public async Task ShouldHaveConsumedAsync<T>() where T : class
        {
            (await received.Any<T>()).ShouldBeTrue();
        }

        public async Task ShouldNotHaveConsumedAsync<T>() where T : class
        {
            (await received.Any<T>()).ShouldBeFalse();
        }

        public async Task ShouldHaveConsumedCountAsync<T>(int expectedCount) where T : class
        {
            await AsyncElementListExtensions.Take(received.SelectAsync<T>(), expectedCount).ToListAsync();
            received.Select<T>().Count().ShouldBe(expectedCount);
        }
    }
}
