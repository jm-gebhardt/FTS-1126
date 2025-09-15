using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Simulation;

/// <inheritdoc />
[UsedImplicitly]
public class ShortLoopSimulator(IBus bus, ILogger<SimulatorBase> logger, SimulateOptions simOptions, KaeserGraphConfiguration kaeserGraphConfiguration) : SimulatorBase(bus, logger, kaeserGraphConfiguration)
{
    /// <inheritdoc />
    protected override List<Tuple<List<string>, List<string>>> SourceDestinationPools { get; } =
    [
        new(["2130", "2230", "2330", "2430"], ["1060"]),
        new(["2130", "2230", "2330", "2430"], ["1050"])
    ];

    /// <inheritdoc />
    public override int PollingInterval { get; } = 360_000 / simOptions.Speed; // 10/h in ms
}