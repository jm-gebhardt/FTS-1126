using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

/// <summary>
/// Provides a consumer that responds <see cref="Success"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
[UsedImplicitly]
public class SuccessResponseConsumer<TRequest> : IConsumer<TRequest>
where TRequest: class
{
    public async Task Consume(ConsumeContext<TRequest> context)
    {
        await context.RespondAsync(new Success());
    }
}