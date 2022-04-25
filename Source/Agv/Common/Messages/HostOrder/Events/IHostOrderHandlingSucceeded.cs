using Gebhardt.StoreWare.Agv.Common.Definitions;

namespace Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events
{
	/// <summary>
	/// An event that reports a successful handling of an order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface IHostOrderHandlingSucceeded
	{
		/// <summary>
		/// The unique ID of the order known by the host system
		/// </summary>
		string OrderId { get; }

		/// <summary>
		/// 
		/// </summary>
		HostOrderErrorCode Error { get; }
	}
}
