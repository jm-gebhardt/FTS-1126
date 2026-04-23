using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Services.CommandHandler;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Gebhardt.Acc.Statistics.Test.Helper;

namespace Gebhardt.Acc.Statistics.Test.Services.CommandHandler;

[TestClass]
public class ErrorCommandHandlerTest
{
    [TestMethod]
    public async Task ConsumeAddError_AgvError_AgvErrorAdded()
    {
        var position = GridCellGenerator.GenerateGridCell();
        MockDbConsumerTestFixture<ErrorCommandHandler> fixture = new MockDbConsumerTestFixture<ErrorCommandHandler>()
            .WithGridCells([position]);
        var error = AgvErrorGenerator.GenerateAgvError();
        await fixture.TestHarness.Bus.Publish(new AddError(error, [position.CellId]));
        await fixture.ShouldHaveConsumedAsync<AddError>();
        fixture.StatisticsDbMock.VerifyErrorAdded();
    }

    [TestMethod]
    public async Task ConsumeAddError_FieldViolation_AgvFieldViolationAdded()
    {
        var position = GridCellGenerator.GenerateGridCell();
        MockDbConsumerTestFixture<ErrorCommandHandler> fixture = new MockDbConsumerTestFixture<ErrorCommandHandler>()
            .WithGridCells([position]);
        var fieldViolation = new FieldViolation(Guid.NewGuid(), DateTime.UtcNow);
        await fixture.TestHarness.Bus.Publish(new AddError(fieldViolation, [position.CellId]));
        await fixture.ShouldHaveConsumedAsync<AddError>();
        fixture.StatisticsDbMock.VerifyFieldViolationAdded();
    }
}