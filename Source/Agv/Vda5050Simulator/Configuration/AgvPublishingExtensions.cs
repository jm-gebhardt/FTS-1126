using Gebhardt.Acc.Vda5050Simulator.Messaging;
using Gebhardt.Acc.Vda5050Simulator.Runtime;
using Gebhardt.Acc.Vda5050Simulator.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Gebhardt.Acc.Vda5050Simulator.Configuration;

public static class AgvPublishingExtensions
{
    /// <summary>
    /// Registers the state cache and the <see cref="IAgvPublisher"/> chain.
    /// Order (outermost first): SignalR, cache, MQTT. The cache is filled before SignalR clients are notified.
    /// </summary>
    public static IServiceCollection AddAgvPublishing(this IServiceCollection services)
    {
        services.AddSingleton<MqttSendQueue>();
        services.AddSingleton<IAgvStateCache, AgvStateCache>();
        services.AddSingleton<IAgvPublisher>(sp =>
        {
            IAgvPublisher publisher = sp.GetRequiredService<MqttSendQueue>();
            publisher = new CachingAgvPublisher(publisher, sp.GetRequiredService<IAgvStateCache>());
            publisher = new SignalRAgvPublisher(
                publisher,
                sp.GetRequiredService<IHubContext<SimulatedAgvStateHub, ISimulatedAgvStateClient>>(),
                sp.GetRequiredService<IOptions<MqttOptions>>(),
                sp.GetRequiredService<ILogger<SignalRAgvPublisher>>());
            return publisher;
        });
        return services;
    }
}
