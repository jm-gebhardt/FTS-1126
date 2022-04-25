using Gebhardt.StoreWare.Agv.Common.Definitions;

namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events
{
	/// <summary>
	/// An event that reports the failed cancel of an order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface ITransportOrderCancelFailed
	{
		/// <summary>
		/// The unique ID of the order known by the host system
		/// </summary>
		string OrderId { get; }

		/// <summary>
		/// The error code that holds information why the cancel of the order failed
		/// </summary>
		TransportOrderErrorCode ErrorCode { get; }
	}
}
