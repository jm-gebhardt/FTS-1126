using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.SimulationMode;

/// <inheritdoc />
public class LongLoopSimulator(IBus bus, ILogger<SimulatorBase> logger, SimulateOptions simOptions) : SimulatorBase(bus, logger)
{
    /// <inheritdoc />
    protected override List<Tuple<List<string>,List<string>>> SourceDestinationPools { get; } = [new Tuple<List<string>, List<string>>(["9060"], ["1060"])];

    /// <inheritdoc />
    public override int PollingInterval { get; } = 90000 / simOptions.Speed; // 40/h in ms
}