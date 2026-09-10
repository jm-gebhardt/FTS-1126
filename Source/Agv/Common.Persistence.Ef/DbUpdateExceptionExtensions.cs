using JetBrains.Annotations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Common.Persistence.Ef;

/// <summary>
/// Provides extension methods for <see cref="DbUpdateException"/>.
/// </summary>
[PublicAPI]
public static class DbUpdateExceptionExtensions
{
    private const int UniqueIndexViolation = 2601;

    private const int UniqueConstraintViolation = 2627;

    /// <param name="exception">The exception raised while saving changes.</param>
    extension(DbUpdateException exception)
    {
        /// <summary>
        /// Indicates whether the save failed because a unique index or a unique constraint was violated.
        /// </summary>
        public bool IsUniqueViolation =>
            exception.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation };
    }
}