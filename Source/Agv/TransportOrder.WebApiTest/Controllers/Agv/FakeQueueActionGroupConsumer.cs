using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Commands;
using Gebhardt.StoreWare.Agv.Common.Testing.RequestHandlers;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers.Agv;

[UsedImplicitly]
public class FakeQueueActionGroupConsumer : SuccessRequestHandlerMock<QueueActionGroup>;