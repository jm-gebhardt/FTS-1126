using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Testing.RequestHandlers;

/// <summary>
/// Provides a consumer that responds <see cref="Success"/>.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
[UsedImplicitly]
public class SuccessRequestHandlerMock<TRequest>() : RequestHandlerMock<TRequest, Success>(new Success())
    where TRequest : class;