using Gebhardt.StoreWare.Agv.Common.Testing.RequestHandlers;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.RequestHandler.ActionGroup;

[UsedImplicitly]
public class AbortActionGroupFailedRequestHandler() : RequestHandlerMock<AbortActionGroup, AbortActionGroupFailed>(new AbortActionGroupFailed("Abort action failed."));

[UsedImplicitly]
public class AbortActionGroupSuccessRequestHandler : SuccessRequestHandlerMock<AbortActionGroup>;