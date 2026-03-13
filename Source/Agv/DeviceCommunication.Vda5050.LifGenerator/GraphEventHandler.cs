using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using MassTransit;
using Vdma.Lif;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.LifGenerator;

/// <summary>
/// Handles graph related events
/// </summary>
public class GraphEventHandler:
    IConsumer<AgvGraphReceived>
{
    /// <inheritdoc/>
    public Task Consume(ConsumeContext<AgvGraphReceived> context)
    {
        var layout = context.Message.Graph.ToLifLayout();
        var layoutCollection = new LifLayoutCollection()
        {
            MetaInformation = new MetaInformation()
            {
                Creator = "Gebhardt",
                LifVersion = "1.1",
                ExportTimestamp = DateTime.UtcNow,
                ProjectIdentification = "ACC"
            },
            Layouts = [layout]
        };

        layoutCollection.ToFile($"./Configuration/{context.Message.SerialNumber}.json", true);
        return Task.CompletedTask;
    }
}