namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands
{

	/// <summary>
	/// A command from the transport order module to the agv communication module to execute an order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface IExecuteTransportOrder
	{
		/// <summary>
		/// The unique ID of the order known by the host system
		/// </summary>
		string OrderId { get; }

		/// <summary>
		/// 
		/// </summary>
		string AssignedAgv { get; }

		/// <summary>
		/// The source station of the order in the format that is known by the agv 
		/// </summary>
		string SourceId { get; }

		/// <summary>
		/// The destination station of the order in the format that is known by the agv
		/// </summary>
		string DestinationId { get; }
	}
}