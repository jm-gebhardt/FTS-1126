using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.StationCommunication.Messages.Stations;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.StationCommunication.Services.EventHandler;

/// <summary>
/// Provides a handler for station related events.
/// </summary>
[UsedImplicitly]
public class StationEventHandler : IConsumer<StationConfigurationUpdateRequested>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<StationConfigurationUpdateRequested> context)
    {
        await context.Publish(new ReloadStationConfiguration());
    }
}