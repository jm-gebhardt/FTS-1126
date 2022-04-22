using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events
{
	/// <summary>
	/// An event that reports the successful completion of an order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface ITransportOrderCompleted: CorrelatedBy<Guid>
	{
		/// <summary>
		/// The unique ID of the order known by the host system
		/// </summary>
		string OrderId { get; }
	}
}
