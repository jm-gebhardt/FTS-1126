using Gebhardt.Acc.Common.Domain.Utilities;
using MassTransit;

namespace Gebhardt.Acc.Common.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="IPartitioner"/>.
/// </summary>
public static class PartitionerExtension
{
    /// <summary>
    /// Partition the given message by its CorrelationId.
    /// </summary>
    /// <typeparam name="TMessage">The message to be partitioned.</typeparam>
    /// <typeparam name="TConsumer">The message consumer.</typeparam>
    /// <param name="partitioner">The partitioner.</param>
    /// <param name="c">The consumer configurator.</param>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IPartitioner PartitionMessageById<TMessage, TConsumer>(this IPartitioner partitioner, IConsumerConfigurator<TConsumer> c)
        where TMessage : class, CorrelatedBy<Guid>
        where TConsumer : class
    {
        c.Message<TMessage>(x => x.UsePartitioner(partitioner, p => p.Message.CorrelationId));
        return partitioner;
    }

    /// <inheritdoc cref="PartitionMessageById{TMessage,TConsumer}"/>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IPartitioner PartitionMessageByString<TMessage, TConsumer>(this IPartitioner partitioner, IConsumerConfigurator<TConsumer> c)
        where TMessage : class, CorrelatedBy<string>
        where TConsumer : class
    {
        c.Message<TMessage>(x => x.UsePartitioner(partitioner, p => p.Message.CorrelationId.ToGuid()));
        return partitioner;
    }
}