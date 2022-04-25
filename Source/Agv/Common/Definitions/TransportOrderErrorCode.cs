namespace Gebhardt.StoreWare.Agv.Common.Definitions
{
	/// <summary>
	/// Error code that can occur during handling of transport orders
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public enum TransportOrderErrorCode
	{
		/// <summary>
		/// Dummy value for any error
		/// </summary>
		Any,
		/// <summary>
		/// Error code that reports an abortion of the order by an user input
		/// </summary>
		AbortedByUser
	}
}
