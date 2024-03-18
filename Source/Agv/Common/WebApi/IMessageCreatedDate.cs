using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Common.WebApi;

/// <summary>
/// A Dto with a timestamp showing when the Dto was created.
/// </summary>
public interface IMessageCreatedDate
{
    /// <summary>
    /// The date when the Dto was created.
    /// </summary>
    [PublicAPI]
    DateTime MessageCreatedDate { get; }
}