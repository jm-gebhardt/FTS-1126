using Gebhardt.Acc.DeviceCommunication.SimAgv.CmdOptions;

namespace Gebhardt.Acc.DeviceCommunicationTest.Helper;

public static class SimulateOptionsGenerator
{
    public static Options GenerateSimulateOptions(string source, string destination, int numberOfSimulatedAgvs = 1, int nodeTravelTime = 5000)
    {
        return GenerateSimulateOptions(new[] { source }, new[] { destination }, numberOfSimulatedAgvs, nodeTravelTime);
    }

    private static Options GenerateSimulateOptions(string[] sources, string[] destinations, int numberOfSimulatedAgvs = 1, int nodeTravelTime = 5000)
    {
        return new Options
        {
            Agvs = numberOfSimulatedAgvs,
            Sources = sources,
            Destinations = destinations,
            SimulationSpeed = nodeTravelTime
        };
    }
}