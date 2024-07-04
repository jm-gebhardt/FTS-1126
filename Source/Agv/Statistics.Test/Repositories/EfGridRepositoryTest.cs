using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Statistics.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Repositories;

[TestClass]
public class EfGridRepositoryTest
{
    [TestMethod]
    public async Task ThrowIfNotExistAsync_GridDoesNotExist_ThrowsGridNotFoundException()
    {
        await using var fixture = new StatisticsDbTestFixture();
        await Should.ThrowAsync<GridNotFoundException>(fixture.Grids.ThrowIfNotExistAsync("unknown".ToGuid()));
    }
}