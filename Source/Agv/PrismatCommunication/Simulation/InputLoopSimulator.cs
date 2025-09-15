using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Simulation;

/// <inheritdoc />
[UsedImplicitly]
public class InputLoopSimulator(IBus bus, ILogger<SimulatorBase> logger, SimulateOptions simOptions, KaeserGraphConfiguration kaeserGraphConfiguration) : SimulatorBase(bus, logger, kaeserGraphConfiguration)
{
    /// <inheritdoc />
    protected override List<Tuple<List<string>, List<string>>> SourceDestinationPools { get; } =
    [
        new(["1040"], ["9070"]),
        new(["1020"], ["9070"]),
        new(["1040"], ["2110", "2210", "2310", "2410"]),
        new(["1020"], ["2110", "2210", "2310", "2410"])
    ];

    /// <inheritdoc />
    public override int PollingInterval { get; } = 240_000 / simOptions.Speed; // 15/h in ms
}