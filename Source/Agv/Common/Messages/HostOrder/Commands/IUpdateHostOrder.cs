using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands
{
	/// <summary>
	/// A command from the host system to update an existing transport order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface IUpdateHostOrder: CorrelatedBy<Guid>
	{
		/// <summary>
		/// Updated order details
		/// </summary>
		Order Order { get; }
	}
}
