using Gebhardt.StoreWare.Agv.Common.Definitions;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events
{
	/// <summary>
	/// An event that reports the failed update of an order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface ITransportOrderUpdateFailed: CorrelatedBy<Guid>
	{
		/// <summary>
		/// The unique ID of the order known by the host system
		/// </summary>
		string OrderId { get; }

		/// <summary>
		/// The error code that holds information why the update of the order failed
		/// </summary>
		TransportOrderErrorCode ErrorCode { get; }
	}
}
