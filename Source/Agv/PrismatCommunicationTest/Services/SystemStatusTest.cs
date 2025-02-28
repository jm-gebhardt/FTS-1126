using Gebhardt.Acc.PrismatCommunication.Messages.Commands.SystemStatus;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.PrismatCommunicationTest.Services;

[TestClass]
public class SystemStatusTest
{
    [TestMethod]
    public async Task Consume_GetSystemStatus_NoExistingStatus_ReportsNotReadyStatus()
    {
        using var fixture = new SystemStateCommandHandlerTestFixture().WithSystemStatus(null);

        await fixture.TestHarness.Bus.Publish(new GetSystemStatus());

        await fixture.ShouldHaveConsumedAsync<GetSystemStatus>();

        fixture.VerifyReportSystemStatus(false, Times.Once());
    }

    [TestMethod]
    public async Task Consume_GetSystemStatus_ExistingStatus_ReportsStatus()
    {
        var status = new PrismatCommunication.Domain.Models.SystemStatus.SystemStatus(true, 0, "");
        using var fixture = new SystemStateCommandHandlerTestFixture().WithSystemStatus(status);

        await fixture.TestHarness.Bus.Publish(new GetSystemStatus());

        await fixture.ShouldHaveConsumedAsync<GetSystemStatus>();

        fixture.VerifyReportSystemStatus(status.IsReady, Times.Once());
    }

    [TestMethod]
    public async Task Consume_SetReady_NoExistingStatus_ReportsReadyStatus()
    {
        using var fixture = new SystemStateCommandHandlerTestFixture().WithSystemStatus(null);

        await fixture.TestHarness.Bus.Publish(new SetReady());

        await fixture.ShouldHaveConsumedAsync<SetReady>();

        fixture.VerifyReportSystemStatus(true, Times.Once());
    }

    [TestMethod]
    public async Task Consume_SetReady_ExistingStatusReady_DoesNotReport()
    {
        var status = new PrismatCommunication.Domain.Models.SystemStatus.SystemStatus(true, 0, "");
        using var fixture = new SystemStateCommandHandlerTestFixture().WithSystemStatus(status);

        await fixture.TestHarness.Bus.Publish(new SetReady());

        await fixture.ShouldHaveConsumedAsync<SetReady>();

        fixture.VerifyReportSystemStatus(true, Times.Never());
    }

    [TestMethod]
    public async Task Consume_SetNotReady_NoExistingStatus_ReportsNotReadyStatus()
    {
        using var fixture = new SystemStateCommandHandlerTestFixture().WithSystemStatus(null);

        await fixture.TestHarness.Bus.Publish(new SetNotReady());

        await fixture.ShouldHaveConsumedAsync<SetNotReady>();

        fixture.VerifyReportSystemStatus(false, Times.Once());
    }

    [TestMethod]
    public async Task Consume_SetNotReady_ExistingStatusNotReady_DoesNotReport()
    {
        var status = new PrismatCommunication.Domain.Models.SystemStatus.SystemStatus(false, 42, "an error");
        using var fixture = new SystemStateCommandHandlerTestFixture().WithSystemStatus(status);

        await fixture.TestHarness.Bus.Publish(new SetNotReady());

        await fixture.ShouldHaveConsumedAsync<SetNotReady>();

        fixture.VerifyReportSystemStatus(false, Times.Never());
    }

    [TestMethod]
    public async Task Consume_SetNotReady_ExistingStatusReady_ReportsNotReadyStatus()
    {
        var status = new PrismatCommunication.Domain.Models.SystemStatus.SystemStatus(true, 0, "");
        using var fixture = new SystemStateCommandHandlerTestFixture().WithSystemStatus(status);

        await fixture.TestHarness.Bus.Publish(new SetNotReady());

        await fixture.ShouldHaveConsumedAsync<SetNotReady>();

        fixture.VerifyReportSystemStatus(false, Times.Once());
    }
}