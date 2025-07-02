using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Contracts;

namespace Gebhardt.Acc.Statistics.Messages.Events;

/// An event indicating the persistence of the Agv position in the grids is done.
/// <param name="SerialNumber">The serial number of the AGV.</param>
/// <param name="AgvPosition">The current position of the AGV.</param>
/// <param name="GridPositionIds">The IDs of the grid positions associated to the position.</param>
public record GridPositionPersisted(string SerialNumber, AgvPosition AgvPosition, List<Guid> GridPositionIds) : IAgvSerialNumber, IWithAgvPosition;