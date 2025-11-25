using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Moq;

namespace Gebhardt.Acc.Statistics.Test.Fixtures;

/// <summary>
/// Provides extensions for the mock db.
/// </summary>
public static class MockDbExtension
{
    /// <summary>
    /// Extension block for <see cref="Mock{IStatisticsDb}"/>.
    /// </summary>
    extension(Mock<IStatisticsDb> dbMock)
    {
        /// <summary>
        /// Setup verifiable methods.
        /// </summary>
        public void SetupVerifiableMethods()
        {
            dbMock.Setup(x => x.Grids.Add(It.IsAny<Grid>())).Verifiable();
            dbMock.Setup(x => x.GridAgvFieldViolations.Add(It.IsAny<AgvFieldViolation>())).Verifiable();
            dbMock.Setup(x => x.GridAgvErrors.Add(It.IsAny<AgvError>())).Verifiable();
            dbMock.Setup(x => x.Maps.Add(It.IsAny<Map>())).Verifiable();
            dbMock.Setup(x => x.GridCell.GetOrAddGridCellsForCoordinatesAsync(It.IsAny<decimal>(), It.IsAny<decimal>())).Verifiable();
            dbMock.Setup(x => x.MapGridCell.Add(It.IsAny<MapGridCell>())).Verifiable();
            dbMock.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>())).Verifiable();
        }

        /// <summary>
        /// Verify a grid was added.
        /// </summary>
        public void VerifyGridAdded()
        {
            dbMock.Verify(x => x.Grids.Add(It.IsAny<Grid>()), Times.AtLeastOnce);
            dbMock.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Verify an error was added.
        /// </summary>
        public void VerifyErrorAdded()
        {
            dbMock.Verify(x => x.GridAgvErrors.Add(It.IsAny<AgvError>()), Times.AtLeastOnce);
            dbMock.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Verify a field violation was added.
        /// </summary>
        public void VerifyFieldViolationAdded()
        {
            dbMock.Verify(x => x.GridAgvFieldViolations.Add(It.IsAny<AgvFieldViolation>()), Times.AtLeastOnce);
            dbMock.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}