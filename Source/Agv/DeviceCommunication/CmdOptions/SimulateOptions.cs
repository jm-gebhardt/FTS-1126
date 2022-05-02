using CommandLine;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.CmdOptions
{
	/// <summary>
	/// CommandLine Options for Simulation Mode
	/// </summary>
	[Verb("simulate", HelpText = "Run in simulation mode")]
	public sealed class SimulateOptions
	{
		/// <summary>
		/// Response delay interval (seconds) in which the IExecuteTransportOrder is completed by sending ITransportOrderCompleted
		/// </summary>
		[Option("interval", HelpText = "Response delay interval in seconds", Required = true, Min = 2, Max = 2, Separator = ',')]
		public IEnumerable<int>? Interval { get; init; }

	}
}
