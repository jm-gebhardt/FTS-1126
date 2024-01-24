using Gebhardt.StoreWare.Agv.Statistics.Services.RequestResponseHandler;
using MassTransit;
using Statistics.Messages.Requests;

namespace Gebhardt.StoreWare.Agv.Statistics.Configuration;

/// <summary>
/// Provides extension methods for the mediator registration.
/// </summary>
public static class MediatorRegistrationExtension
{
    /// <summary>
    /// Add mediator consumers.
    /// </summary>
    public static void AddMediatorConsumers(this IMediatorRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<GridRequestHandler>();
        cfg.AddRequestClient<GetGrids>();
        cfg.AddRequestClient<GetPositionFrequenciesForGrid>();
    }
}
