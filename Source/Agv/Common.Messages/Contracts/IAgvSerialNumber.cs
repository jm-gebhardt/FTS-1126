using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.Common.Messages.Contracts;

/// <summary>
/// Interface for AGV identification.
/// </summary>
public interface IAgvSerialNumber
{
    /// <summary>
    /// The serial number of the agv.
    /// It is unique for the whole system.
    /// </summary>
    /// <remarks>
    /// For internal correlation the ID is transformed into a Guid (see <see cref="StringExtensions.ToGuid"/>
    /// </remarks>
    string SerialNumber { get; }
}