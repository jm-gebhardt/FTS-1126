using Gebhardt.StoreWare.Agv.Common.Definitions;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events
{
	/// <summary>
	/// An event that reports a failure while handling an order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface IHostOrderHandlingFailed: CorrelatedBy<Guid>
	{
		/// <summary>
		/// The unique ID of the order known by the host system
		/// </summary>
		string OrderId { get; }

		/// <summary>
		/// The error code that holds information why the handling of the order failed
		/// </summary>
		HostOrderErrorCode Error { get; }
	}
}
