namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands.SystemStatus;

/// <summary>
/// Provides a command to decide, if the agv limit at akl stations is used.
/// </summary>
/// <param name="UseLimit">Flag indicating if the limit is used.</param>
public record UseAklAgvLimit(bool UseLimit);