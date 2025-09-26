using Gebhardt.Acc.Common.Filter.FilterableParser;
using Gebhardt.Acc.Common.Filter.FilterableParser.Attributes;
using Gebhardt.Acc.Common.Persistence.Ef;
using Gebhardt.Acc.Common.Testing.Fixtures;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gebhardt.Acc.CommonTest.Filter;

public class FilterExpressionHelperTestFixture : DbTestFixtureBase<TestDbContext>;

public class TestDbEntity()
{
    public Guid Key { get; init; } = Guid.NewGuid();

    public string StringValue { get; init; } = "";

    public int IntValue { get; init; }

    public int? NullablePrimitive { get; init; }

    public Dictionary<string, string> Converted { get; init; } = new();

    public TestDbEntity(string stringValue, int intValue) : this()
    {
        StringValue = stringValue;
        IntValue = intValue;
    }
}

public class TestDbEntityReadModel : TestDbEntity, IFilterable<TestDbEntity, TestDbEntityReadModel>
{
    [KeyProperty]
    [FilterProperty]
    public new Guid Key { get; init; } = Guid.NewGuid();

    [FilterProperty]
    [UsedImplicitly]
    public new string StringValue { get; init; } = "";

    [FilterProperty]
    [UsedImplicitly]
    public new int IntValue { get; init; }

    [FilterProperty]
    [UsedImplicitly]
    public new int? NullablePrimitive { get; init; }

    [HasConversion]
    [UsedImplicitly]
    public new Dictionary<string, string> Converted { get; init; } = new();
}

public class TestCollectionEntity()
{
    public Guid Key { get; init; } = Guid.NewGuid();

    public List<TestDbEntity> Values { get; init; } = [];

    public TestCollectionEntity(List<TestDbEntity> values) : this()
    {
        Values = values;
    }
}

public class TestCollectionEntityReadModel : TestCollectionEntity, IFilterable<TestCollectionEntity, TestCollectionEntityReadModel>
{
    [KeyProperty]
    [UsedImplicitly]
    public new Guid Key { get; init; } = Guid.NewGuid();

    [NavigationProperty]
    public new IEnumerable<TestDbEntityReadModel> Values { get; [UsedImplicitly] init; } = [];
}

public class TestJoinSourceEntity()
{
    public Guid TjsKey { get; } = Guid.NewGuid();

    public Guid? TjtFKey { get; init; } = Guid.Empty;

    public bool Exists { get; init; } = true;

    public TestJoinSourceEntity(Guid targetKey) : this()
    {
        TjtFKey = targetKey;
    }
}

public class TestJoinSourceReadModel : TestJoinSourceEntity, IFilterable<TestJoinSourceEntity, TestJoinSourceReadModel>
{
    [KeyProperty]
    [UsedImplicitly]
    public new Guid TjsKey { get; init; } = Guid.NewGuid();

    public Guid? TjtFKeyKey { get; init; }

    [JoinProperty(nameof(TjtFKeyKey), nameof(TestJoinTargetEntity.TjTKey), true)]
    public new TestJoinTargetReadModel? TjtFKey { get; init; } = null!;

    [FilterProperty]
    [UsedImplicitly]
    public new bool Exists { get; init; }
}

public class TestJoinTargetEntity()
{
    public Guid TjTKey { get; init; } = Guid.NewGuid();

    public int Value { get; } 

    public TestJoinTargetEntity(int value) : this()
    {
        Value = value;
    }
}

public class TestJoinTargetReadModel : TestJoinTargetEntity, IFilterable<TestJoinTargetEntity, TestJoinTargetReadModel>
{
    [KeyProperty]
    [UsedImplicitly]
    public new Guid TjTKey { get; init; } = Guid.NewGuid();

    [FilterProperty]
    public new int Value { get; init; } 
}

public class TestJoinSourceEntityConfiguration : IEntityTypeConfiguration<TestJoinSourceEntity>
{
    public void Configure(EntityTypeBuilder<TestJoinSourceEntity> builder)
    {
        builder.Property(e => e.TjtFKey);
        builder.HasKey(e => e.TjsKey);
    }
}

public class TestJoinTargetEntityConfiguration : IEntityTypeConfiguration<TestJoinTargetEntity>
{
    public void Configure(EntityTypeBuilder<TestJoinTargetEntity> builder)
    {
        builder.Property(e => e.Value);
        builder.HasKey(e => e.TjTKey);
    }
}

public class TestDbEntityConfiguration : IEntityTypeConfiguration<TestDbEntity>
{
    public void Configure(EntityTypeBuilder<TestDbEntity> builder)
    {
        builder.Property(e => e.IntValue);
        builder.Property(e => e.StringValue);
        builder.Property(e => e.NullablePrimitive);
        builder.Property(e => e.Converted).HasJsonConversion();
        builder.HasKey(e => e.Key);
    }
}

public class TestCollectionEntityConfiguration : IEntityTypeConfiguration<TestCollectionEntity>
{
    public void Configure(EntityTypeBuilder<TestCollectionEntity> builder)
    {
        builder.HasMany<TestDbEntity>(e => e.Values);
        builder.HasKey(e => e.Key);
    }
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TestDbEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TestCollectionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TestJoinSourceEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TestJoinTargetEntityConfiguration());
    }
}