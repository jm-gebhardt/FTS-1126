using Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;

public static class SimulateOptionsGenerator
{
    public static SimulateOptions GenerateSimulateOptions(string source, string destination, int numberOfSimulatedAgvs = 1, int nodeTravelTime = 5000)
    {
        return GenerateSimulateOptions(new[] {source}, new[] {destination}, numberOfSimulatedAgvs, nodeTravelTime);
    }

    private static SimulateOptions GenerateSimulateOptions(string[] sources, string[] destinations, int numberOfSimulatedAgvs = 1, int nodeTravelTime = 5000)
    {
        return new SimulateOptions
        {
            Agvs = numberOfSimulatedAgvs,
            Sources = sources,
            Destinations = destinations,
            SimulationSpeed = nodeTravelTime
        };
    }
}