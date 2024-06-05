using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Services.CommandHandler;
using Gebhardt.Acc.Statistics.Test.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.Statistics.Test.Services.CommandHandler;

[TestClass]
public class ErrorCommandHandlerTest
{
    [TestMethod]
    public async Task ConsumeAddError_AgvError_AgvErrorAdded()
    {
        var position = new GridAgvPosition(Guid.NewGuid(), "agv", 1, 1, 1, 1, 1, DateTime.UtcNow, null);
        MockDbConsumerTestFixture<ErrorCommandHandler> fixture = new MockDbConsumerTestFixture<ErrorCommandHandler>()
            .WithGridPositions([ position]);
        var error = AgvErrorGenerator.GenerateAgvError();
        await fixture.TestHarness.Bus.Publish(new AddError(error, position.PositionId));
        (await fixture.ConsumerHarness.Consumed.Any<AddError>()).ShouldBeTrue();
        fixture.StatisticsDbMock.VerifyErrorAdded();
    }

    [TestMethod]
    public async Task ConsumeAddError_FieldViolation_AgvFieldViolationAdded()
    {
        var position = new GridAgvPosition(Guid.NewGuid(), "agv", 1, 1, 1, 1, 1, DateTime.UtcNow, null);
        MockDbConsumerTestFixture<ErrorCommandHandler> fixture = new MockDbConsumerTestFixture<ErrorCommandHandler>()
            .WithGridPositions([position]);
        var fieldViolation = new FieldViolation(Guid.NewGuid(), DateTime.UtcNow);
        await fixture.TestHarness.Bus.Publish(new AddError(fieldViolation, position.PositionId));
        (await fixture.ConsumerHarness.Consumed.Any<AddError>()).ShouldBeTrue();
        fixture.StatisticsDbMock.VerifyFieldViolationAdded();
    }
}