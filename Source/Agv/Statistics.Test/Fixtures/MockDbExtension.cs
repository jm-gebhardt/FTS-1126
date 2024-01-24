using Moq;
using Statistics.Domain.Models;
using Statistics.Domain.Repositories;
using Range = Moq.Range;

namespace Statistics.Test.Fixtures;

/// <summary>
/// Provides extensions for the mock db.
/// </summary>
public static class MockDbExtension
{
    /// <summary>
    /// Setup verifiable methods.
    /// </summary>
    /// <param name="dbMock">The db mock.</param>
    public static void SetupVerifiableMethods(this Mock<IStatisticsDb> dbMock)
    {
        dbMock.Setup(x => x.Grids.Add(It.IsAny<Grid>())).Verifiable();
        dbMock.Setup(x => x.Maps.Add(It.IsAny<Map>())).Verifiable();
        dbMock.Setup(x => x.GridPositions.Add(It.IsAny<AgvGridPosition>())).Verifiable();
        dbMock.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>())).Verifiable();
    }

    /// <summary>
    /// Verify agv positions were added.
    /// </summary>
    /// <param name="dbMock"></param>
    /// <param name="count">The number of positions added.</param>
    public static void VerifyAgvPositionsAdded(this Mock<IStatisticsDb> dbMock, int count)
    {
        dbMock.Verify(x => x.GridPositions.Add(It.IsAny<AgvGridPosition>()), Times.Between(1, count, Range.Inclusive));
        dbMock.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}