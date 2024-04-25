using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Represents the event raised when a new error was received for an agv.
/// </summary>
/// <param name="Error">The error.</param>
/// <param name="Position">The position the error was received at.</param>
/// <param name="SerialNumber">The agv serial number.</param>
public record AgvErrorReceived(AgvError Error, AgvPosition Position, string SerialNumber);