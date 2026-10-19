using Gebhardt.Acc.Common.WebApi;

namespace Gebhardt.Acc.Vda5050Simulator.WebApi.Middleware;

/// <inheritdoc/>
public class ExceptionHandlerMiddleware(ILogger<BaseExceptionHandlerMiddleware> logger) : BaseExceptionHandlerMiddleware(logger);
