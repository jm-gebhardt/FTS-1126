using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Testing.RequestHandlers;

/// <summary>
/// Provides a consumer that responds <see cref="Success"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
[UsedImplicitly]
public class SuccessRequestHandlerMock<TRequest>() : RequestHandlerMock<TRequest, Success>(new Success())
    where TRequest : class;