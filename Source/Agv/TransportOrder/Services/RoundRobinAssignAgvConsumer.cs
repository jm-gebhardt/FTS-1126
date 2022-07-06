using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{

	/// <summary>
	/// Round Robin AgvId Dispatcher responding to IAssignAGV
	/// This is a very simple implementation which is not aware of the AGV current state
	/// </summary>
	[JetBrains.Annotations.PublicAPI]
	public class RoundRobinAssignAgvConsumer : IConsumer<IAssignAgv>
	{
		private const string AgvNamePrefix = "AGV";
		private readonly RoundRobinAgvRegistry _agvRegistry;
		/// <summary>
		/// Round Robin AGV Dispatcher
		/// </summary>
		public RoundRobinAssignAgvConsumer(RoundRobinAgvRegistry registry)
		{
			_agvRegistry = registry;
		}

		/// <summary>
		/// Respond to AGV Assignment in a RoundRobin fashion
		/// </summary>
		/// <param name="context"></param>
		public async Task Consume(ConsumeContext<IAssignAgv> context)
		{
			await context.Publish<IAgvAssignmentSucceeded>(new
			{
				context.Message.CorrelationId,
				AgvId = ChooseAgv()
			});
		}

		private string ChooseAgv()
		{
			return $"{AgvNamePrefix}_{_agvRegistry.Next()}";
		}
	}
}