using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Testing.RequestHandlers;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Agv;

[UsedImplicitly]
public class FakeQueueActionGroupConsumer : SuccessRequestHandlerMock<QueueActionGroup>;