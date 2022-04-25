namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands
{

	/// <summary>
	/// A command from the transport order module to the agv communication module to cancel an order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface ICancelTransportOrder
	{
		/// <summary>
		/// The unique ID of the order known by the host system
		/// </summary>
		string OrderId { get; }

		/// <summary>
		/// The agv that was assigned for this transport by the transport order module
		/// </summary>
		string AssignedAgv { get; }
	}
}
