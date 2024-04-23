using Gebhardt.Acc.Common.Testing.RequestHandlers;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Events.ActionGroup;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Test.Services.RequestHandler.ActionGroup;

[UsedImplicitly]
public class AbortActionGroupFailedRequestHandler() : RequestHandlerMock<AbortActionGroup, AbortActionGroupFailed>(new AbortActionGroupFailed("Abort action failed."));

[UsedImplicitly]
public class AbortActionGroupSuccessRequestHandler : SuccessRequestHandlerMock<AbortActionGroup>;