using Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
	/// <summary>
	/// A simple registry to save state of assigned AGV Id
	/// </summary>
	public class RoundRobinAgvRegistry
	{
		private int _current;
		private readonly int _agvCount;
		private readonly object _lockObject = new();

		/// <summary>
		/// A simple registry to save state of assigned AGV Id.
		/// </summary>
		/// <param name="options">The simulation options that hold the amount of available AGVs</param>
		public RoundRobinAgvRegistry(SimulateOptions options)
		{
			_agvCount = options.Agvs;
		}

		/// <summary>
		/// Get next agv Id thread safe
		/// </summary>
		/// <returns></returns>
		public int Next()
		{
			lock (_lockObject)
			{
				int next = _current;
				_current = (_current + 1) % _agvCount;
				return next;
			}
		}
	}
}
