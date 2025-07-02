using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Messages.Requests;
using Gebhardt.Acc.Statistics.Services.CommandHandler;
using Gebhardt.Acc.Statistics.Services.RequestResponseHandler;
using MassTransit;

namespace Gebhardt.Acc.Statistics.Configuration;

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
        cfg.AddConsumer<GridCommandHandler>();
        cfg.AddConsumer<HousekeepingCommandHandler>();
        cfg.AddRequestClient<AddGrid>();
        cfg.AddConsumer<GridRequestHandler>();
        cfg.AddRequestClient<GetGrids>();
        cfg.AddRequestClient<GetPositionFrequencies>();
        cfg.AddRequestClient<GetErrorFrequencies>();
        cfg.AddRequestClient<GetFieldViolationFrequencies>();
        cfg.AddRequestClient<GetWifiSignalStrength>();
    }
}