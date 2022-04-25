using Gebhardt.StoreWare.Agv.Common.Definitions;

namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events
{
	/// <summary>
	/// An event that reports a failed order update
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface IHostOrderUpdateFailed
	{
		/// <summary>
		/// The unique ID of the order known by the host system
		/// </summary>
		string OrderId { get; }

		/// <summary>
		/// The error code that holds information why the update of the order failed
		/// </summary>
		HostOrderErrorCode Error { get; }
	}
}
