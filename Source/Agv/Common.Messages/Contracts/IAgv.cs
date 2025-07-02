using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Messages.Contracts;

/// <summary>
/// Interface for AGV identification for better generalization and simplification of documentation.
/// </summary>
[PublicAPI]
public interface IAgv : IAgvSerialNumber
{
    /// <summary>
    /// The correlation Id of the Agv.
    /// </summary>
    Guid CorrelationId { get; }
}