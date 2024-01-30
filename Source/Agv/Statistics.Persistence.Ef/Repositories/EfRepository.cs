using Gebhardt.StoreWare.Agv.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Statistics.Persistence.Ef.Repositories;

/// <summary>
/// Provides an abstract base class for all repository implementations.
/// </summary>
public abstract class EfRepository<T, TContext> : IRepository<T>
    where T : class
    where TContext : DbContext
{
    private readonly TContext _context;

    /// <summary>
    /// Gets the underlying db set.
    /// </summary>
    protected readonly IQueryable<T> DbSet;

    /// <summary>
    /// Initializes a new <see cref="EfRepository{T,TContext}" /> instance.
    /// </summary>
    protected EfRepository(TContext context)
    {
        _context = context;
        DbSet = _context.Set<T>();
    }

    /// <inheritdoc />
    public async Task<ICollection<T>> GetAll()
    {
        return await DbSet.AsNoTracking().ToListAsync();
    }

    /// <inheritdoc />
    public async Task<T> GetByIdAsync(Guid id)
    {
        var entityType = _context.Model.FindEntityType(typeof(T));
        string? keyName = entityType?.FindPrimaryKey()?.Properties.Select(x => x.Name).Single();

        var parameter = Expression.Parameter(typeof(T), "e");
        Expression<Func<T, bool>> predicate = Expression.Lambda<Func<T, bool>>(
            Expression.Equal(
                Expression.Property(parameter, keyName!),
                Expression.Constant(id)
            ),
            parameter
        );

        return await DbSet.SingleAsync(predicate);
    }

    /// <inheritdoc />
    public void Remove(T aggregate)
    {
        _context.Remove(aggregate);
    }

    /// <inheritdoc />
    public void Add(T aggregate)
    {
        _context.Add(aggregate);
    }

    /// <inheritdoc />
    public void AddRange(IEnumerable<T> aggregates)
    {
        _context.AddRange(aggregates);
    }

    /// <inheritdoc />
    public void AddRange(params T[] aggregates)
    {
        AddRange(aggregates.AsEnumerable());
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        await _context.SaveChangesAsync();
    }
}