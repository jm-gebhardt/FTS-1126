using Gebhardt.Acc.Common.WebApi;

namespace DeviceCommunication.Vda5050.Simulator.WebApi.Middleware;

/// <inheritdoc/>
public class ExceptionHandlerMiddleware(ILogger<BaseExceptionHandlerMiddleware> logger) : BaseExceptionHandlerMiddleware(logger);
