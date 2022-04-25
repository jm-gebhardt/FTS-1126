using Gebhardt.StoreWare.Agv.Common.Definitions;

namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events
{
	/// <summary>
	/// An event that reports a failed cancel of an order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface IHostOrderCancelFailed
	{
		/// <summary>
		/// The unique ID of the order known by the host system
		/// </summary>
		string OrderId { get; }

		/// <summary>
		/// The error code that holds information why the cancel of the order failed
		/// </summary>
		HostOrderErrorCode Error { get; }
	}
}
