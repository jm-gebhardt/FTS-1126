using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

public class AgvTypeRepositoryTestFixture : TransportOrderDbFixture
{
    private const string Manufacturer = "KARIS";
    private const string SeriesName = "BF1";
    public const string AgvTypeIdentifier = "KARIS.BF1";
    public const double UpdatedLength = 3.0;
    public const double UpdatedWidth = 4.0;

    public IAgvTypeRepository Repository { get; }

    public AgvTypeRepositoryTestFixture()
    {
        Repository = new EfAgvTypeRepository(ContextFactory);
    }

    public static AgvType GenerateAgvType(double length = 1.0, double width = 2.0, bool acknowledged = false)
    {
        return new AgvType(AgvTypeIdentifier, Manufacturer, SeriesName, length, width, acknowledged);
    }

    public async Task<AgvType?> GetAsync(string agvTypeIdentifier)
    {
        await using var context = await ContextFactory.CreateDbContextAsync();
        return await context.Set<AgvType>().AsNoTracking().SingleOrDefaultAsync(a => a.AgvTypeIdentifier == agvTypeIdentifier);
    }
}