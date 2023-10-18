using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.Messages;

/// <summary>
/// Marker interface for messages with creation date.
/// </summary>
public interface IMessageCreatedDate
{
    /// <summary>
    /// The message creation date.
    /// </summary>
    [UsedImplicitly]
    DateTime MessageCreatedDate { get; }
}