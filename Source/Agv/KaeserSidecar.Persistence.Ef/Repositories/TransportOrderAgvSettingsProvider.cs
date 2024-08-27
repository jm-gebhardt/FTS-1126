using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.KaeserSidecar.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Models.Settings;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.KaeserSidecar.Persistence.Ef.Repositories;

/// <inheritdoc cref="ITransportOrderAgvSettingsProvider"/>>
public class TransportOrderAgvSettingsProvider : EfAgvSettingsProvider,  ITransportOrderAgvSettingsProvider
{
    /// <inheritdoc cref="ITransportOrderAgvSettingsProvider"/>>
    public TransportOrderAgvSettingsProvider(IDbContextFactory<TransportOrderDbContext> dbContextFactory, IConcurrentDictionaryProvider<Guid, AgvSettingsReadModel> agvSettingsProvider) : base(dbContextFactory, agvSettingsProvider)
    {
    }
}