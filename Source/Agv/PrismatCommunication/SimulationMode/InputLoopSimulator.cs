using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.SimulationMode;

/// <inheritdoc />
public class InputLoopSimulator(IBus bus, ILogger<SimulatorBase> logger, SimulateOptions simOptions) : SimulatorBase(bus, logger)
{
    /// <inheritdoc />
    protected override List<Tuple<List<string>, List<string>>> SourceDestinationPools { get; } = [
        new Tuple<List<string>, List<string>>(["1040"], ["9070"]),
        new Tuple<List<string>, List<string>>(["1020"], ["2110", "2210", "2310", "2410"])
    ];

    /// <inheritdoc />
    public override int PollingInterval { get; } = 240000 / simOptions.Speed; // 15/h in ms
}