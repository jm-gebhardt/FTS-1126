using Gebhardt.Acc.Common.Persistence.Ef;
using JetBrains.Annotations;

namespace Gebhardt.Acc.OrderTranslator.Persistence.Ef.DataAccess;

/// <inheritdoc />
[UsedImplicitly]
public class OrderTranslatorDesignTimeDbContextFactory : DesignTimeDbContextFactory<OrderTranslatorDbContext>
{
    /// <inheritdoc />
    public OrderTranslatorDesignTimeDbContextFactory() : base(OrderTranslatorDbContext.ConfigSectionName, IDbContext<OrderTranslatorDbContext>.MigrationTableName)
    {
    }
}