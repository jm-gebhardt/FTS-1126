using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Common.Persistence.Ef;

/// <summary>
/// Represents a generic database context contract for a specific DbContext type.
/// </summary>
/// <typeparam name="TContext">The specific DbContext type.</typeparam>
public interface IDbContext<TContext>
    where TContext : DbContext
{
    /// <summary>
    /// The configuration section name where the connection string is stored.
    /// </summary>
    [PublicAPI]
    static abstract string ConfigSectionName { get; }

    /// <summary>
    /// The name of the migration table used by EF.
    /// </summary>
    static string MigrationTableName => $"__{typeof(TContext).Name}Migration";
}