using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Contracts;

namespace Gebhardt.Acc.Common.Messages.AgvManagement.Events;

/// <summary>
/// Represents the event raised when a new error was received for an agv.
/// </summary>
/// <param name="Error">The error.</param>
/// <param name="AgvPosition">The position the error was received at.</param>
/// <param name="SerialNumber">The agv serial number.</param>
public record AgvErrorReceived(AgvError Error, AgvPosition AgvPosition, string SerialNumber) : DatedMessage, IAgvSerialNumber, IWithAgvPosition;