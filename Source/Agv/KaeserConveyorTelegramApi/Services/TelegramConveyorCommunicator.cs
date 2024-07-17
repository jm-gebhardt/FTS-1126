using Gebhardt.Acc.KaeserConveyorTelegramApi.Processors;
using Gebhardt.Acc.KaeserSidecar.Domain.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.KaeserConveyorTelegramApi.Services;

/// <inheritdoc cref="IConveyorCommunicator"/>>
[PublicAPI]
#pragma warning disable CS9113 // Parameter is unread.
public class TelegramConveyorCommunicator(ILogger<TelegramConveyorCommunicator> logger, ToConveyorTelegramProcessor telegramProcessor) : IConveyorCommunicator;
#pragma warning restore CS9113 // Parameter is unread.
