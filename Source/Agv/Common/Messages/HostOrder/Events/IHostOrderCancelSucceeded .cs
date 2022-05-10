using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events
{
	/// <summary>
	/// An event that reports a successful cancel of an order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface IHostOrderCancelSucceeded: CorrelatedBy<Guid>
	{
		/// <summary>
		/// The unique ID of the order known by the host system
		/// </summary>
		string OrderId { get; }
	}
}
