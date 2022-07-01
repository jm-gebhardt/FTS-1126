using Microsoft.Extensions.Logging;
using MassTransit;

using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
	/// <summary>
	/// Execute processing of a transport order messages.
	/// </summary>
	[JetBrains.Annotations.UsedImplicitly]
	public class AgvRequestGraphConsumer : IConsumer<IRequestAgvGraph>
	{
		private readonly ILogger<AgvRequestGraphConsumer> _logger;

		/// <summary>
		/// Handle requesting AGV graph by master control.
		/// </summary>
		public AgvRequestGraphConsumer(ILogger<AgvRequestGraphConsumer> logger)
		{
			_logger = logger;
		}

		/// <summary>
		/// Handle requesting AGV graph on receiving message <see cref="IRequestAgvGraph"/> from master control.
		/// </summary>
		/// <param name="context">Context of the consumer wrapping details surrounding the inbound message.</param>
		public async Task Consume(ConsumeContext<IRequestAgvGraph> context)
		{
			await context.PublishMqtt<IRequestGraph>(new
			{
				SerialNumber = context.Message.AgvId,
				RequestId = context.Message.CorrelationId
			}, context.CancellationToken);

			_logger.LogInformation("Requesting AGV graph from AGV {AgvId}.", context.Message.AgvId);

		}
	}
}
