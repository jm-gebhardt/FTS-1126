namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands
{
	/// <summary>
	/// A command from the host system to update an existing transport order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface IUpdateHostOrder
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
	}
}
