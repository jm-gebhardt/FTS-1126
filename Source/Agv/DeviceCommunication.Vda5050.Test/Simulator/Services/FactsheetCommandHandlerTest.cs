using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Commands;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper;
using Shouldly;
using Fixture = Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services.FactsheetCommandHandlerTestFixture;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Simulator.Services;

[TestClass]
public class FactsheetCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_CacheFactsheet_ValidData_CachesFactsheet()
    {
        var fixture = new Fixture().Build();

        await fixture.PublishMessageAsync(new CacheFactsheet(FactsheetJsonGenerator.AgvTypeId, FactsheetJsonGenerator.AgvTypeIdentifier, FactsheetJsonGenerator.FactsheetJson));

        (await fixture.Cache.TryGetAsync(FactsheetJsonGenerator.AgvTypeId)).ShouldBe(FactsheetJsonGenerator.FactsheetJson);
    }

    [TestMethod]
    public async Task Consume_FetchFactsheet_UploadedFactsheet_CachesFactsheet()
    {
        var fixture = new Fixture().WithUploadedFactsheet().Build();

        await fixture.PublishMessageAsync(new FetchFactsheet(FactsheetJsonGenerator.AgvTypeId, FactsheetJsonGenerator.AgvTypeIdentifier));

        (await fixture.Cache.TryGetAsync(FactsheetJsonGenerator.AgvTypeId)).ShouldBe(FactsheetJsonGenerator.FactsheetJson);
    }

    [TestMethod]
    public async Task Consume_FetchFactsheet_NoUploadedFactsheet_DoesNotCacheFactsheet()
    {
        var fixture = new Fixture().WithoutUploadedFactsheet().Build();

        await fixture.PublishMessageAsync(new FetchFactsheet(FactsheetJsonGenerator.AgvTypeId, FactsheetJsonGenerator.AgvTypeIdentifier));

        (await fixture.Cache.TryGetAsync(FactsheetJsonGenerator.AgvTypeId)).ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_FetchFactsheet_RequestFaults_DoesNotCacheFactsheetOrFault()
    {
        var fixture = new Fixture().WithFaultingDeviceCommunication().Build();

        await fixture.PublishMessageAsync(new FetchFactsheet(FactsheetJsonGenerator.AgvTypeId, FactsheetJsonGenerator.AgvTypeIdentifier));

        (await fixture.Cache.TryGetAsync(FactsheetJsonGenerator.AgvTypeId)).ShouldBeNull();
        (await fixture.ConsumerHarness.Consumed.Any<FetchFactsheet>(m => m.Exception != null)).ShouldBeFalse();
    }
}