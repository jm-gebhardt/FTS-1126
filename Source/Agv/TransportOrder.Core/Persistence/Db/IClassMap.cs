using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Persistence.Db
{
    /// <summary>
    /// Map Class to specific type
    /// </summary>
    public interface IClassMap
    {
        /// <summary>
        /// Entity class type
        /// </summary>
        [UsedImplicitly]
        Type ClassType { get; }

        /// <summary>
        /// Entity table name.
        /// Defaults to ClassType.Name
        /// Only set if not empty string
        /// </summary>
        [UsedImplicitly]
        string TableName { get; }

        /// <summary>
        /// Configure the entity
        /// </summary>
        /// <param name="model"></param>
        void Configure(ModelBuilder model);
    }

    /// <summary>
    /// Map Class to specific type
    /// </summary>
    // ReSharper disable once UnusedTypeParameter
    public interface IClassMap<T> : IClassMap
        where T : class
    {
    }
}