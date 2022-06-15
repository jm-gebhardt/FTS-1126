using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands
{
	/// <summary>
	/// A command to the agv communication module to execute a charge order
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public interface ISendAgvChargeOrder : CorrelatedBy<Guid>
	{
		/// <summary>
		/// The AGV that shall go charging
		/// </summary>
		string AgvId { get; }

		/// <summary>
		/// The charger where the AGV shall go to
		/// </summary>
		string ToCharger { get; }

	}
}
