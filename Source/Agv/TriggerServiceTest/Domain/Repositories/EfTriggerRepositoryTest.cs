using Gebhardt.Acc.TriggerService.Domain.Exceptions;
using Gebhardt.Acc.TriggerService.Test.Fixtures;
using Gebhardt.Acc.TriggerService.Test.Helper;
using Shouldly;

namespace Gebhardt.Acc.TriggerService.Test.Domain.Repositories;

[TestClass]
public class EfTriggerRepositoryTest
{
    [TestMethod]
    public async Task Add_GetById_ValidData_TriggerAddedAndReturned()
    {
        var fixture = new EfTriggerRepositoryTestFixture();

        var trigger = TriggerGenerator.GenerateTrigger();

        (await fixture.Repository.TryAdd(trigger.Properties.Id, trigger)).ShouldBeTrue();

        (await fixture.Repository.GetById(trigger.Properties.Id)).ShouldBeEquivalentTo(trigger);
    }

    [TestMethod]
    public async Task Add_GetById_AddOrUpdate_TriggerAddedAndReturned()
    {
        var fixture = new EfTriggerRepositoryTestFixture();

        var trigger = TriggerGenerator.GenerateTrigger();

        await fixture.Repository.AddOrUpdate(trigger.Properties.Id, trigger);
        trigger.UpdateDefaultConfigurationParameter("name", "Value1");
        await fixture.Repository.AddOrUpdate(trigger.Properties.Id, trigger);

        (await fixture.Repository.GetById(trigger.Properties.Id)).ShouldBeEquivalentTo(trigger);
    }

    [TestMethod]
    public async Task GetById_UnknownTrigger_ThrowsTriggerNotFoundException()
    {
        var fixture = new EfTriggerRepositoryTestFixture();
        await Should.ThrowAsync<TriggerNotFoundException>(fixture.Repository.GetById(Guid.NewGuid()));
    }

    [TestMethod]
    public async Task UpdateTrigger_ValidData_TriggerUpdated()
    {
        var fixture = new EfTriggerRepositoryTestFixture();

        var trigger = TriggerGenerator.GenerateTrigger();

        await fixture.Repository.TryAdd(trigger.Properties.Id, trigger);

        trigger.UpdateDefaultConfigurationParameter("name", "newValue");

        await fixture.Repository.UpdateTrigger(trigger.Properties.Id, trigger);

        (await fixture.Repository.GetById(trigger.Properties.Id)).ShouldBeEquivalentTo(trigger);
    }

    [TestMethod]
    public async Task UpdateTrigger_UnknownTriggerId_ThrowsTriggerNotFoundException()
    {
        var fixture = new EfTriggerRepositoryTestFixture();

        var trigger = TriggerGenerator.GenerateTrigger();

        await fixture.Repository.TryAdd(trigger.Properties.Id, trigger);

        trigger.UpdateDefaultConfigurationParameter("name", "newValue");

        await Should.ThrowAsync<TriggerNotFoundException>(fixture.Repository.UpdateTrigger(Guid.NewGuid(), trigger));
    }
}