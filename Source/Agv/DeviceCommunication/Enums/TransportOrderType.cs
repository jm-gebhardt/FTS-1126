using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Enums
{
	/// <summary>
	/// The type of transport an order describes
	/// </summary>
	[PublicAPI]
	public enum TransportOrderType
	{
		/// <summary>
		/// Only pick at FromStation
		/// </summary>
		Pick,
		/// <summary>
		/// Only drop at ToStation
		/// </summary>
		Drop,
		/// <summary>
		/// Do pick at FromStation and drop at ToStation
		/// </summary>
		Transport
	}
}
