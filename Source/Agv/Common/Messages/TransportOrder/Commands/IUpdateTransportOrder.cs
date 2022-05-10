using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands
{

	/// <summary>
	/// A command from the transport order module to the agv communication module to update an order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface IUpdateTransportOrder : CorrelatedBy<Guid>
	{
		/// <summary>
		/// Order Details
		/// </summary>
		Order Order { get; }

		/// <summary>
		/// The assigned AGV
		/// </summary>
		string AssignedAgv { get; }
	}
}