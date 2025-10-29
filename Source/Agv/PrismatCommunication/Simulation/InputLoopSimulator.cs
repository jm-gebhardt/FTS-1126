using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.Simulation;

/// <inheritdoc />
[UsedImplicitly]
public class InputLoopSimulator(IBus bus, ILogger<SimulatorBase> logger, SimulateOptions simOptions, KaeserGraphConfiguration kaeserGraphConfiguration, bool includeAplTargets = true) : SimulatorBase(bus, logger, kaeserGraphConfiguration)
{
    /// <inheritdoc />
    protected override List<Tuple<List<string>, List<string>>> SourceDestinationPools
    {
        get
        {
            var pools = new List<Tuple<List<string>, List<string>>>();
            if (includeAplTargets)
            {
                pools.AddRange(
                [
                    new Tuple<List<string>, List<string>>(["1040"], ["9070"]),
                    new Tuple<List<string>, List<string>>(["1020"], ["9070"])
                ]);
            }

            pools.AddRange(
                [
                    new Tuple<List<string>, List<string>>(["1040"], ["2110", "2210", "2310", "2410"]),
                    new Tuple<List<string>, List<string>>(["1020"], ["2110", "2210", "2310", "2410"])
                ]
            );

            return pools;
        }
    }

    /// <inheritdoc />
    public override int PollingInterval { get; } = 240_000 / simOptions.Speed; // 15/h in ms
}