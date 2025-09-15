using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Simulation;

/// <inheritdoc />
[UsedImplicitly]
public class LongLoopSimulator(IBus bus, ILogger<SimulatorBase> logger, SimulateOptions simOptions, KaeserGraphConfiguration kaeserGraphConfiguration) : SimulatorBase(bus, logger, kaeserGraphConfiguration)
{
    /// <inheritdoc />
    protected override List<Tuple<List<string>, List<string>>> SourceDestinationPools { get; } = [new(["9060"], ["1060"]), new(["9060"], ["1050"])];

    /// <inheritdoc />
    public override int PollingInterval { get; } = 90_000 / simOptions.Speed; // 40/h in ms
}