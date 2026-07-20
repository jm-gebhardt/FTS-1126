using Gebhardt.Acc.Common.WebApi;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Middleware;

/// <inheritdoc/>
public class ExceptionHandlerMiddleware(ILogger<BaseExceptionHandlerMiddleware> logger) : BaseExceptionHandlerMiddleware(logger);