using Gebhardt.StoreWare.Agv.Common.Definitions;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands
{
	/// <summary>
	/// A command from the host system to create a transport order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface IHandleHostOrder: CorrelatedBy<Guid>
	{
		/// <summary>
		/// The unique ID of the order known by the host system
		/// </summary>
		string OrderId { get; }

		/// <summary>
		/// The source where to pick up the order
		/// </summary>
		string Source { get; }

		/// <summary>
		/// The destination where to drop the order
		/// </summary>
		string Destination { get; }

		/// <summary>
		/// The LeNo of the orders handled box or transportation good
		/// </summary>
		string LeNo { get; }

		/// <summary>
		/// The creator of the host order
		/// </summary>
		HostOrderCreator Creator { get; }
	}
}
