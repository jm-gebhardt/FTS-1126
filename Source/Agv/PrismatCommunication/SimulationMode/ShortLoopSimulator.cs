using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.SimulationMode;

/// <inheritdoc />
public class ShortLoopSimulator(IBus bus, ILogger<SimulatorBase> logger, SimulateOptions simOptions) : SimulatorBase(bus, logger)
{
    /// <inheritdoc />
    protected override List<Tuple<List<string>, List<string>>> SourceDestinationPools { get; } = [new Tuple<List<string>, List<string>>(["2130", "2230", "2330", "2430"], ["1050"])];

    /// <inheritdoc />
    public override int PollingInterval { get; } = 360000/ simOptions.Speed; // 10/h in ms
}