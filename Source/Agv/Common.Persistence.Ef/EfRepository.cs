using System.Linq.Expressions;
using Gebhardt.Acc.Common.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Common.Persistence.Ef;

/// <summary>
/// Provides an abstract base class for all repository implementations.
/// </summary>
public abstract class EfRepository<T, TContext> : IRepository<T>
    where T : class
    where TContext : DbContext
{
    /// <summary>
    /// The db context.
    /// </summary>
    protected readonly TContext Context;

    /// <summary>
    /// Gets the underlying db set.
    /// </summary>
    protected readonly IQueryable<T> DbSet;

    private readonly bool _selfManaged;

    /// <summary>
    /// Initializes a new <see cref="EfRepository{T,TContext}" /> instance.
    /// </summary>
    protected EfRepository(TContext context)
    {
        Context = context;
        DbSet = Context.Set<T>();
    }

    /// <summary>
    /// Initializes a new <see cref="EfRepository{T,TContext}" /> instance that is self-managed.
    /// </summary>
    protected EfRepository(TContext context, bool selfManaged) : this(context)
    {
        _selfManaged = selfManaged;
    }

    /// <inheritdoc />
    public async Task<ICollection<T>> GetAll()
    {
        return await DbSet.AsNoTracking().ToListAsync();
    }

    /// <inheritdoc />
    public async Task<T> GetByIdAsync(Guid id)
    {
        var entityType = Context.Model.FindEntityType(typeof(T));
        string? keyName = entityType?.FindPrimaryKey()?.Properties.Select(x => x.Name).Single();

        var parameter = Expression.Parameter(typeof(T), "e");
        var predicate = Expression.Lambda<Func<T, bool>>(
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
        Context.Remove(aggregate);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(T aggregate)
    {
        Context.Remove(aggregate);
        await Context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public void Add(T aggregate)
    {
        Context.Add(aggregate);
    }

    /// <inheritdoc />
    public async Task AddAsync(T aggregate)
    {
        Context.Add(aggregate);
        await Context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public void AddRange(IEnumerable<T> aggregates)
    {
        Context.AddRange(aggregates);
    }

    /// <inheritdoc />
    public void AddRange(params T[] aggregates)
    {
        AddRange(aggregates.AsEnumerable());
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        await Context.SaveChangesAsync();
        if (!_selfManaged)
        {
            Context.ChangeTracker.Clear();
        }
    }
}

/// <summary>
/// Provides an abstract base class for all repository implementations.
/// </summary>
public abstract class EfRepository<T, TContext, TContextFactory> : IRepository<T>
    where T : class
    where TContext: DbContext
    where TContextFactory : IDbContextFactory<TContext>
{
    /// <summary>
    /// The db context.
    /// </summary>
    protected readonly TContextFactory ContextFactory;

    /// <summary>
    /// Initializes a new <see cref="EfRepository{T,TContext, TContextFactory}" /> instance.
    /// </summary>
    protected EfRepository(TContextFactory contextFactory)
    {
        ContextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<ICollection<T>> GetAll()
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        
        return await context.Set<T>().AsNoTracking().ToListAsync();
    }

    /// <inheritdoc />
    public async Task<T> GetByIdAsync(Guid id)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        var entityType = context.Model.FindEntityType(typeof(T));
        string? keyName = entityType?.FindPrimaryKey()?.Properties.Select(x => x.Name).Single();

        var parameter = Expression.Parameter(typeof(T), "e");
        var predicate = Expression.Lambda<Func<T, bool>>(
            Expression.Equal(
                Expression.Property(parameter, keyName!),
                Expression.Constant(id)
            ),
            parameter
        );

        return await context.Set<T>().SingleAsync(predicate);
    }

    /// <inheritdoc />
    public void Remove(T aggregate)
    {
        using var context = ContextFactory.CreateDbContext();
        context.Remove(aggregate);
        context.SaveChanges();
    }

    /// <inheritdoc />
    public async Task RemoveAsync(T aggregate)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        context.Remove(aggregate);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public void Add(T aggregate)
    {
        using var context = ContextFactory.CreateDbContext();
        context.Add(aggregate);
        context.SaveChanges();
    }

    /// <inheritdoc />
    public async Task AddAsync(T aggregate)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        context.Add(aggregate);
        await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public void AddRange(IEnumerable<T> aggregates)
    {
        using var context = ContextFactory.CreateDbContext();
        context.AddRange(aggregates);
        context.SaveChanges();
    }

    /// <inheritdoc />
    public void AddRange(params T[] aggregates)
    {
        AddRange(aggregates.AsEnumerable());
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        await context.SaveChangesAsync();
    }
}