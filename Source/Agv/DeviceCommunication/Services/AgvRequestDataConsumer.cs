using Microsoft.Extensions.Logging;
using MassTransit;

using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
	/// <summary>
	/// Execute processing of a agv data request
	/// </summary>
	[JetBrains.Annotations.UsedImplicitly]
	public class AgvRequestDataConsumer : IConsumer<RequestAgvData>
	{
		private readonly ILogger<AgvRequestDataConsumer> _logger;

		/// <summary>
		/// Handle requesting AGV data by master control.
		/// </summary>
		public AgvRequestDataConsumer(ILogger<AgvRequestDataConsumer> logger)
		{
			_logger = logger;
		}

		/// <summary>
		/// Handle requesting AGV data on receiving message <see cref="RequestAgvData"/> from master control.
		/// </summary>
		/// <param name="context">Context of the consumer wrapping details surrounding the inbound message.</param>
		public async Task Consume(ConsumeContext<RequestAgvData> context)
		{
			await context.PublishMqtt<IRequestData>(new
			{
				SerialNumber = context.Message.AgvId,
				RequestId = context.Message.CorrelationId
			}, context.CancellationToken);

			_logger.LogInformation("Requesting AGV data from AGV {AgvId} / {CorrelationId}", context.Message.AgvId, context.CorrelationId);

		}
	}
}
