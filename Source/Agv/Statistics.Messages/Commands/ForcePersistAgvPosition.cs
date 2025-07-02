using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.Contracts;

namespace Gebhardt.Acc.Statistics.Messages.Commands;

/// <summary>
/// A command to force persist the current position.
/// </summary>
/// <param name="SerialNumber">The serial number of the AGV.</param>
/// <param name="AgvPosition">The current position of the AGV.</param>
public record ForcePersistAgvPosition(string SerialNumber, AgvPosition AgvPosition) : DatedMessage, IAgvSerialNumber;