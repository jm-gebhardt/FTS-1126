using Microsoft.Extensions.Hosting;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Graph;

/// <summary>
/// Loads the graph into the <see cref="GraphManager"/> on startup.
/// </summary>
internal sealed class GraphInitializer(GraphManager graphManager) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return graphManager.InitializeAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}