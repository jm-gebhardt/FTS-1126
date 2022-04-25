namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events
{
	/// <summary>
	/// An event that reports the successful partial completion of an order. The transport good was picked at the source station.
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface ITransportOrderPickCompleted
	{
		/// <summary>
		/// The unique ID of the order known by the host system
		/// </summary>
		string OrderId { get; }
	}
}
