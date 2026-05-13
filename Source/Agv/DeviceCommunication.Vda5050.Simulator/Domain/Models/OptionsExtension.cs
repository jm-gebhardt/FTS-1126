using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Configuration;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;

/// <summary>
/// Provides extension methods for <see cref="Options"/>
/// </summary>
public static class OptionsExtension
{
    extension(Options options)
    {
        /// <summary>
        /// True, if the given manufacture and serial number matches an agv simulated by this service.
        /// </summary>
        public bool IsSimulatedAgv(string manufacturer, string serialNumber)
        {
            return string.Equals(options.Manufacturer, manufacturer, StringComparison.InvariantCultureIgnoreCase) && options.SimulatedSerialNumbers().Contains(serialNumber);
        }

        /// <summary>
        /// Get all simulated serial  numbers
        /// </summary>
        /// <returns></returns>
        public string[] SimulatedSerialNumbers()
        {
            return Enumerable.Range(0, options.Agvs).Select(o => $"{options.SerialNumberBase}{o}").ToArray();
        }
    }
}