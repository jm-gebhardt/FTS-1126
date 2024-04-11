using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Testing.RequestHandlers;

/// <summary>
/// Directly responds with the saved object
/// </summary>
/// <typeparam name="TRequest">The request type to handle</typeparam>
/// <typeparam name="TResponse">The response type to send</typeparam>
[UsedImplicitly]
public class RequestHandlerMock<TRequest, TResponse> : IConsumer<TRequest> where TResponse : class where TRequest : class
{
    private readonly TResponse _response;

    /// <summary>
    /// <inheritdoc cref="RequestHandlerMock{TRequest,TResponse}"/>
    /// </summary>
    /// <param name="response">The object to send when the request arrives</param>
    public RequestHandlerMock(TResponse response)
    {
        _response = response;
    }

    /// <inheritdoc />>
    public async Task Consume(ConsumeContext<TRequest> context)
    {
        await context.RespondAsync(_response);
    }
}