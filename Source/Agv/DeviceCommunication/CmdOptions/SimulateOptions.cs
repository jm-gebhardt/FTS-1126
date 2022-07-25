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
		/// Number of AGVs
		/// </summary>
		[JetBrains.Annotations.UsedImplicitly]
		// ReSharper disable once StringLiteralTypo
		[Option('c', "agvs", HelpText = "Number of simulated AGVs", Default=1)]
		// ReSharper disable once IdentifierTypo
		public int Agvs { get;  init; }
		
		/// <summary>
		/// List of simulation sources
		/// </summary>
		[JetBrains.Annotations.UsedImplicitly]
		[Option('s', "sources", HelpText = "List of simulation sources", Required = true, Min = 1, Separator = ',')]
		public IEnumerable<string>? Sources { get; init; }

		/// <summary>
		/// List of simulation destinations
		/// </summary>
		[JetBrains.Annotations.UsedImplicitly]
		[Option('d', "destinations", HelpText = "List of simulation destinations", Required = true, Min = 1, Separator = ',')]
		public IEnumerable<string>? Destinations { get; init; }
	}
}
