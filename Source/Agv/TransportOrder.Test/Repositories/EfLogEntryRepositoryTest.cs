using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfLogEntryRepositoryTest
{
    [TestMethod]
    public async Task FilterAsync_ValidData_ReturnsLogEntries()
    {
        const string logLevel = "Information";
        var agv = Guid.NewGuid();
        await using var fixture = new EfLogEntryRepositoryTestFixture();
        await fixture.Repository.AddAsync(Categories.Order, 310, logLevel, agv, null, null, DateTime.UtcNow, new Dictionary<string, object?>{{"State_Operation", 132}});

        var logEntries = await fixture.Repository.FilterAsync(new CombinationFilter(
            LogicalCombinationVariant.And,
            [new ValueComparisonFilter<Guid>(nameof(LogEntry.AgvCorrelationId), ComparisonVariant.Equals, agv),
                new ValueComparisonFilter<string>(nameof(LogEntry.LogLevel), ComparisonVariant.Equals, logLevel)
            ]), 0, 10, CancellationToken.None);
        logEntries.Items.Count.ShouldBe(1);
    }
}