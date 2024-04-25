using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.Statistics.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Repositories;

[TestClass]
public class EfMapRepositoryTest
{
    [TestMethod]
    public async Task ThrowIfNotExistAsync_MapDoesNotExist_ThrowsMapNotFoundException()
    {
        await using var fixture = new StatisticsDbTestFixture();

        await Should.ThrowAsync<MapNotFoundException>(fixture.Maps.ThrowIfNotExistAsync("unknown".ToGuid()));
    }
}