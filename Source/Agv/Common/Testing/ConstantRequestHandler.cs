using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Testing;

/// <summary>
/// Directly responds with the saved object
/// </summary>
/// <typeparam name="TRequest">The request type to handle</typeparam>
/// <typeparam name="TResponse">The response type to send</typeparam>
public class ConstantRequestHandler<TRequest, TResponse> : IConsumer<TRequest> where TResponse : class where TRequest : class
{
    private readonly TResponse _response;

    /// <summary>
    /// <inheritdoc cref="ConstantRequestHandler{TRequest, TResponse}"/>
    /// </summary>
    /// <param name="response">The object to send when the request arrives</param>
    public ConstantRequestHandler(TResponse response)
    {
        _response = response;
    }

    /// <inheritdoc />>
    public async Task Consume(ConsumeContext<TRequest> context)
    {
        await context.RespondAsync(_response);
    }
}