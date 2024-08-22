using Gebhardt.Acc.PrismatCommunication.CmdOptions;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.PrismatCommunication.SimulationMode;

/// <inheritdoc />
public class ShortLoopSimulator(IBus bus, ILogger<SimulatorBase> logger, SimulateOptions simOptions, KaeserGraphConfiguration kaeserGraphConfiguration) : SimulatorBase(bus, logger, kaeserGraphConfiguration)
{
    /// <inheritdoc />
    protected override List<Tuple<List<string>, List<string>>> SourceDestinationPools { get; } = [new Tuple<List<string>, List<string>>(["2130", "2230", "2330", "2430"], ["1050"])];

    /// <inheritdoc />
    public override int PollingInterval { get; } = 360000/ simOptions.Speed; // 10/h in ms
}