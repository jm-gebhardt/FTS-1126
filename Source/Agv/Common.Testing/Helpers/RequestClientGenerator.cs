using MassTransit;
using Moq;

// ReSharper disable UnusedMember.Global
namespace Gebhardt.StoreWare.Agv.Common.Testing.Helpers;

#region DummyResponse
/// <summary>
/// This dummy implementation is necessary because mocked responses are not working for some reason
/// </summary>
public class DummyResponse<TRequest> : Response<TRequest>
    where TRequest : class
{
    private readonly TRequest _message;
    private readonly Guid? _correlationId;

    public Guid? MessageId => _correlationId;

    public Guid? RequestId => _correlationId;

    public Guid? CorrelationId => _correlationId;

    public Guid? ConversationId => _correlationId;

    public Guid? InitiatorId => _correlationId;

    public DateTime? ExpirationTime => DateTime.MaxValue;

    public Uri SourceAddress => new("SourceAddress");

    public Uri DestinationAddress => new("DestinationAddress");

    public Uri ResponseAddress => new("ResponseAddress");

    public Uri FaultAddress => new("FaultAddress");

    public DateTime? SentTime => DateTime.Now;

    public Headers Headers => new Mock<Headers>().Object;

    public HostInfo Host => new Mock<HostInfo>().Object;

    object Response.Message => _message;

    public TRequest Message => _message;

    public DummyResponse(TRequest message)
    {
        _message = message;
        _correlationId = (message as CorrelatedBy<Guid>)?.CorrelationId;
    }
}
#endregion

/// <summary>
/// Generator for dummy request clients - <see cref="IRequestClient{TRequest}"/>
/// </summary>
/// <typeparam name="TRequest">Message type for IRequestClient</typeparam>
public static class RequestClientGenerator<TRequest>
    where TRequest : class
{
    public enum ResponseToUse
    {
        First,
        Second,
        Third
    }

    /// <summary>
    ///  Create dummy <see cref="IRequestClient{TRequest}"/>
    /// </summary>
    /// <typeparam name="TResponse1">Type for first response</typeparam>
    /// <param name="response1">First response</param>
    /// <param name="exception">[Optional] throw this Exception if not null</param>
    public static IRequestClient<TRequest> CreateDummyRequestClient<TResponse1>(TResponse1 response1, Exception? exception = null)
        where TResponse1 : class
    {
        var mockRequestClient = new Mock<IRequestClient<TRequest>>();
        mockRequestClient.Setup(m =>
                m.GetResponse<TResponse1>(It.IsAny<TRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(() =>
            {
                if (exception != null)
                {
                    throw exception;
                }

                return new DummyResponse<TResponse1>(response1);
            });

        return mockRequestClient.Object;
    }

    /// <summary>
    ///  Create dummy <see cref="IRequestClient{TRequest}"/>
    /// </summary>
    /// <typeparam name="TResponse1">Type for first response</typeparam>
    /// <typeparam name="TResponse2">Type of second response</typeparam>
    /// <param name="response1">First response</param>
    /// <param name="response2">Second response</param>
    /// <param name="useResponse2">Set to true if second response should be used</param>
    public static IRequestClient<TRequest> CreateDummyRequestClient<TResponse1, TResponse2>(TResponse1 response1, TResponse2 response2, bool useResponse2 = false)
        where TResponse1 : class
        where TResponse2 : class
    {
        var task1 = new Task<Response<TResponse1>>(() => new DummyResponse<TResponse1>(response1));
        var task2 = new Task<Response<TResponse2>>(() => new DummyResponse<TResponse2>(response2));

        if (!useResponse2) // Decide which response should be returned
        {
            task1.RunSynchronously();
        }
        else
        {
            task2.RunSynchronously();
        }

        var mockRequestClient = new Mock<IRequestClient<TRequest>>();
        mockRequestClient.Setup(m =>
                m.GetResponse<TResponse1, TResponse2>(It.IsAny<TRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(new Response<TResponse1, TResponse2>(task1, task2));

        return mockRequestClient.Object;
    }

    /// <summary>
    ///  Create dummy <see cref="IRequestClient{TRequest}"/>
    /// </summary>
    /// <typeparam name="TResponse1">Type for first response</typeparam>
    /// <typeparam name="TResponse2">Type of second response</typeparam>
    /// <typeparam name="TResponse3">Type of third response</typeparam>
    /// <param name="response1">First response</param>
    /// <param name="response2">Second response</param>
    /// <param name="response3">Third response</param>
    /// <param name="responseToUse">Decides which response should be used</param>
    public static IRequestClient<TRequest> CreateDummyRequestClient<TResponse1, TResponse2, TResponse3>(
        TResponse1 response1,
        TResponse2 response2,
        TResponse3 response3,
        ResponseToUse responseToUse)
        where TResponse1 : class
        where TResponse2 : class
        where TResponse3 : class
    {
        var task1 = new Task<Response<TResponse1>>(() => new DummyResponse<TResponse1>(response1));
        var task2 = new Task<Response<TResponse2>>(() => new DummyResponse<TResponse2>(response2));
        var task3 = new Task<Response<TResponse3>>(() => new DummyResponse<TResponse3>(response3));

        switch (responseToUse) // Decide which response should be returned
        {
            case ResponseToUse.First:
                task1.RunSynchronously();
                break;

            case ResponseToUse.Second:
                task2.RunSynchronously();
                break;

            case ResponseToUse.Third:
                task3.RunSynchronously();
                break;
        }

        var mockRequestClient = new Mock<IRequestClient<TRequest>>();
        mockRequestClient.Setup(m =>
                m.GetResponse<TResponse1, TResponse2, TResponse3>(It.IsAny<TRequest>(), It.IsAny<CancellationToken>(), It.IsAny<RequestTimeout>()))
            .ReturnsAsync(new Response<TResponse1, TResponse2, TResponse3>(task1, task2, task3));

        return mockRequestClient.Object;
    }
}