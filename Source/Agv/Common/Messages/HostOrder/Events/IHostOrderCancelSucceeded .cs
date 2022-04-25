namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events
{
	/// <summary>
	/// An event that reports a successful cancel of an order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface IHostOrderCancelSucceeded
	{
		/// <summary>
		/// The unique order ID
		/// </summary>
		string OrderId { get; }
	}
}
