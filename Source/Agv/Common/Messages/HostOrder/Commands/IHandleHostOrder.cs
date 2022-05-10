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
		/// Order Details
		/// </summary>
		Order Order { get; }
	}
}

