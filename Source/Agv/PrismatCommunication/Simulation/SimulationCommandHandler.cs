using Gebhardt.Acc.Common.Messages.System;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Simulation;

/// <summary>
/// Handles simulation commands to pause or resume order generation.
/// </summary>
[UsedImplicitly]
public class SimulationCommandHandler :
    IConsumer<PauseOrderGeneration>,
    IConsumer<ResumeOrderGeneration>
{
    private readonly IEnumerable<IHostOrderCreator> _hostOrderCreators;
    private readonly ILogger<SimulationCommandHandler> _logger;

    /// <inheritdoc cref="SimulationCommandHandler"/>
    public SimulationCommandHandler(IEnumerable<IHostOrderCreator> hostOrderCreators, ILogger<SimulationCommandHandler> logger)
    {
        _hostOrderCreators = hostOrderCreators;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<PauseOrderGeneration> context)
    {
        foreach (var pollable in _hostOrderCreators)
        {
            pollable.Pause();
        }
        _logger.LogInformation("Order generation paused");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<ResumeOrderGeneration> context)
    {
        foreach (var pollable in _hostOrderCreators)
        {
            pollable.Resume();
        }
        _logger.LogInformation("Order generation resumed");
        return Task.CompletedTask;
    }
}