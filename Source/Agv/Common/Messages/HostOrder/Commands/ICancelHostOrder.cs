namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands
{
	/// <summary>
	/// A command from the host system to cancel an existing transport order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface ICancelHostOrder
	{
		/// <summary>
		/// The unique ID of the order known by the host system
		/// </summary>
		string OrderId { get; }
	}
}
