using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.DataAccess;
using Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Repositories;

[TestClass]
public class AgvEfRegistryTest
{
    private class EfAgvRegistryTestFixture
    {
        public AgvRepository Registry { get; }

        private IDbContextFactory<TransportOrderDbContext> ContextFactory { get; }

        private ServiceProvider Provider { get; }

        public EfAgvRegistryTestFixture()
        {
            var collection = new ServiceCollection()
                .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); }
                );
            Provider = collection.BuildServiceProvider(true);
            ContextFactory = Provider.GetService<IDbContextFactory<TransportOrderDbContext>>()!;
            Registry = new AgvRepository(ContextFactory);
        }

        public async Task Add(TransportOrder.Domain.Models.Agv.Agv agv)
        {
            await Registry.AddAgvAsync(agv);
        }
    }

    [TestMethod]
    public void EmptyRegistry_NoAGVAvailable()
    {
        var fixture = new EfAgvRegistryTestFixture();

        fixture.Registry.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task OneIdleAdded_OneIdleAGVAvailable()
    {
        var fixture = new EfAgvRegistryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();

        await fixture.Add(agv);

        fixture.Registry.Count.ShouldBe(1);

        var readyAgv = (await fixture.Registry.GetIdleAgvsAsync()).First();
        readyAgv.CorrelationId.ShouldBe(agv.CorrelationId);
        readyAgv.CurrentAgvState.ShouldBe(agv.CurrentAgvState);
        readyAgv.AgvId.ShouldBe(agv.AgvId);
        readyAgv.AgvStatus.ShouldBe(agv.AgvStatus);
        readyAgv.IsOnline.ShouldBeFalse();
    }

    [TestMethod]
    public async Task TwoAdded_OneIdleAGVAvailable()
    {
        var fixture = new EfAgvRegistryTestFixture();

        var idleAgv = AgvGenerator.GenerateIdleAgv();
        await fixture.Add(idleAgv);
        var chargingAgv = AgvGenerator.GenerateChargingAgv(1);
        await fixture.Add(chargingAgv);

        fixture.Registry.Count.ShouldBe(2);

        var readyAgv = (await fixture.Registry.GetIdleAgvsAsync()).First();
        readyAgv.CorrelationId.ShouldBe(idleAgv.CorrelationId);
        readyAgv.CurrentAgvState.ShouldBe(idleAgv.CurrentAgvState);
        readyAgv.AgvId.ShouldBe(idleAgv.AgvId);
        readyAgv.AgvStatus.ShouldBe(idleAgv.AgvStatus);
    }

    [TestMethod]
    public async Task Verify_IAgvRegistry_Interface_Methods()
    {
        var fixture = new EfAgvRegistryTestFixture();

        var agv1 = AgvGenerator.GenerateIdleAgv();
        var agv2 = AgvGenerator.GenerateChargingAgv(1);

        await fixture.Add(agv1);
        await fixture.Add(agv2);

        var agvRegistry = fixture.Registry;

        fixture.Registry.Count.ShouldBe(2);

        IEnumerable<TransportOrder.Domain.Models.Agv.Agv> allAgvs = await agvRegistry.GetAllAgvsAsync();
        IEnumerable<TransportOrder.Domain.Models.Agv.Agv> readyAgvs = await agvRegistry.GetIdleAgvsAsync();

        // ReSharper disable PossibleMultipleEnumeration
        allAgvs.Count().ShouldBe(2);
        readyAgvs.Count().ShouldBe(1);

        foreach (var actualAgv in await agvRegistry.GetAllAgvsAsync())
        {
            var foundAgv = await agvRegistry.GetAgvAsync(actualAgv.CorrelationId);
            foundAgv.ShouldNotBeNull();
            foundAgv.AgvId.ShouldBe(actualAgv.AgvId);
        }

        await Should.ThrowAsync<AgvNotFoundException>(agvRegistry.GetAgvAsync(Guid.Empty));
    }

    [TestMethod]
    public async Task CheckIfAgvExistsAsync_ExistingAgv_ReturnsTrue()
    {
        var fixture = new EfAgvRegistryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        await fixture.Add(agv);

        bool agvExists = await fixture.Registry.CheckIfAgvExistsAsync(agv.CorrelationId);
        agvExists.ShouldBeTrue();
    }

    [TestMethod]
    public async Task CheckIfAgvExistsAsync_NonExistingAgv_ReturnsFalse()
    {
        var fixture = new EfAgvRegistryTestFixture();

        bool agvExists = await fixture.Registry.CheckIfAgvExistsAsync("".ToGuid());
        agvExists.ShouldBeFalse();
    }
}