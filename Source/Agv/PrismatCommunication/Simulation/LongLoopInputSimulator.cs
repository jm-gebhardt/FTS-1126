using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Simulation;

/// <inheritdoc />
[UsedImplicitly]
public class LongLoopInputSimulator(IBus bus, ILogger<SimulatorBase> logger, SimulateOptions simOptions)
    : SimulatorBase(bus, logger)
{
    /// <inheritdoc />
    protected override List<Tuple<List<string>, List<string>>> SourceDestinationPools { get; } =
    [
        new(["1040"], ["9070"]),
        new(["1020"], ["9070"])
    ];

    /// <inheritdoc />
    public override int PollingInterval { get; } = 240_000 / simOptions.Speed; // 15/h in ms
}