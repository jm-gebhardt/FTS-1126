using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement
{
	/// <summary>
	/// IOrder is a read-only interface to provide current order data
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface IOrder : CorrelatedBy<Guid>
	{
		/// <summary>
		/// Current State
		/// </summary>
		public string CurrentState { get; }

		/// <summary>
		/// AgvId
		/// </summary>
		public Guid? AssignedAgv { get; }

		/// <summary>
		/// The order details
		/// </summary>
		public OrderDetails OrderDetails { get; }
	}
}
