using CommandLine;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions
{
	/// <summary>
	/// CommandLine Options for Simulation Mode
	/// </summary>
	[JetBrains.Annotations.UsedImplicitly]
	[Verb("simulate", HelpText = "Run in simulation mode")]
	public sealed class SimulateOptions
	{
		/// <summary>
		/// Response delay interval (seconds) in which the ExecuteTransportOrder is completed by sending TransportOrderCompleted
		/// </summary>
		[JetBrains.Annotations.UsedImplicitly]
		[Option('i', "interval", HelpText = "Response delay interval in seconds, e.g. \"5,10\".", Required = true, Min = 2, Max = 2, Separator = ',')]
		public IEnumerable<int>? Interval { get; init; }
	}
}
