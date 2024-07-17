using Gebhardt.Acc.Common.Domain.Repositories;
using JetBrains.Annotations;

namespace Gebhardt.Acc.KaeserSidecar.Domain.Repositories;

/// <summary>
/// Provides a unit-of-work for transport order db access.
/// </summary>
[PublicAPI]
public interface ITransportOrderDb: IUnitOfWork
{
    /// <summary>
    /// The <see cref="ITransportOrderActionGroupRepository"/>.
    /// </summary>
    ITransportOrderActionGroupRepository ActionGroups { get; }

    /// <summary>
    /// The <see cref="ITransportOrderAgvRepository"/>.
    /// </summary>
    ITransportOrderAgvRepository Agvs { get; }
}