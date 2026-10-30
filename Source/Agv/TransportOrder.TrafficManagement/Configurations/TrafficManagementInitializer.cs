using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Agvs;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Configurations;

internal sealed class TrafficManagementInitializer(GraphProvider graphProvider, AgvProvider agvProvider) : IStartupService
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await graphProvider.InitializeAsync(cancellationToken);
        await agvProvider.InitializeAsync(cancellationToken);
    }
}