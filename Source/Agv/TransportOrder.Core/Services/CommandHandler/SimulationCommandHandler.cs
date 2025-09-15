using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Simulation;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Simulation;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Handles simulation-related commands.
/// </summary>
[UsedImplicitly]
public class SimulationCommandHandler(IBus bus) : IConsumer<UpdateSimulationSpeed>
{
    /// <summary>
    /// Handles the <see cref="UpdateSimulationSpeed"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<UpdateSimulationSpeed> context)
    {
        await bus.Publish(new SetSimulationSpeed(context.Message.SimulationSpeed));
        await context.RespondAsync(new Success());
    }
}