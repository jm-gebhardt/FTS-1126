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
public class AgvEfRepositoryTest
{
    private class EfAgvRepositoryTestFixture
    {
        public AgvRepository Repository { get; }

        private IDbContextFactory<TransportOrderDbContext> ContextFactory { get; }

        private ServiceProvider Provider { get; }

        public EfAgvRepositoryTestFixture()
        {
            var collection = new ServiceCollection()
                .AddDbContextFactory<TransportOrderDbContext>(options => { options.UseInMemoryDatabase(Guid.NewGuid().ToString()); }
                );
            Provider = collection.BuildServiceProvider(true);
            ContextFactory = Provider.GetService<IDbContextFactory<TransportOrderDbContext>>()!;
            Repository = new AgvRepository(ContextFactory);
        }

        public async Task Add(TransportOrder.Domain.Models.Agv.Agv agv)
        {
            await Repository.AddAgvAsync(agv);
        }
    }

    [TestMethod]
    public void EmptyRepository_NoAGVAvailable()
    {
        var fixture = new EfAgvRepositoryTestFixture();

        fixture.Repository.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task OneIdleAdded_OneIdleAGVAvailable()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();

        await fixture.Add(agv);

        fixture.Repository.Count.ShouldBe(1);

        var readyAgv = (await fixture.Repository.GetIdleAgvsAsync()).First();
        readyAgv.CorrelationId.ShouldBe(agv.CorrelationId);
        readyAgv.CurrentAgvState.ShouldBe(agv.CurrentAgvState);
        readyAgv.AgvId.ShouldBe(agv.AgvId);
        readyAgv.AgvStatus.ShouldBe(agv.AgvStatus);
        readyAgv.IsOnline.ShouldBeFalse();
    }

    [TestMethod]
    public async Task TwoAdded_OneIdleAGVAvailable()
    {
        var fixture = new EfAgvRepositoryTestFixture();

        var idleAgv = AgvGenerator.GenerateIdleAgv();
        await fixture.Add(idleAgv);
        var chargingAgv = AgvGenerator.GenerateChargingAgv(1);
        await fixture.Add(chargingAgv);

        fixture.Repository.Count.ShouldBe(2);

        var readyAgv = (await fixture.Repository.GetIdleAgvsAsync()).First();
        readyAgv.CorrelationId.ShouldBe(idleAgv.CorrelationId);
        readyAgv.CurrentAgvState.ShouldBe(idleAgv.CurrentAgvState);
        readyAgv.AgvId.ShouldBe(idleAgv.AgvId);
        readyAgv.AgvStatus.ShouldBe(idleAgv.AgvStatus);
    }

    [TestMethod]
    public async Task Verify_IAgvRepository_Interface_Methods()
    {
        var fixture = new EfAgvRepositoryTestFixture();

        var agv1 = AgvGenerator.GenerateIdleAgv();
        var agv2 = AgvGenerator.GenerateChargingAgv(1);

        await fixture.Add(agv1);
        await fixture.Add(agv2);

        var agvRepository = fixture.Repository;

        fixture.Repository.Count.ShouldBe(2);

        IEnumerable<TransportOrder.Domain.Models.Agv.Agv> allAgvs = await agvRepository.GetAllAgvsAsync();
        IEnumerable<TransportOrder.Domain.Models.Agv.Agv> readyAgvs = await agvRepository.GetIdleAgvsAsync();

        // ReSharper disable PossibleMultipleEnumeration
        allAgvs.Count().ShouldBe(2);
        readyAgvs.Count().ShouldBe(1);

        foreach (var actualAgv in await agvRepository.GetAllAgvsAsync())
        {
            var foundAgv = await agvRepository.GetAgvAsync(actualAgv.CorrelationId);
            foundAgv.ShouldNotBeNull();
            foundAgv.AgvId.ShouldBe(actualAgv.AgvId);
        }

        await Should.ThrowAsync<AgvNotFoundException>(agvRepository.GetAgvAsync(Guid.Empty));
    }

    [TestMethod]
    public async Task CheckIfAgvExistsAsync_ExistingAgv_ReturnsTrue()
    {
        var fixture = new EfAgvRepositoryTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        await fixture.Add(agv);

        bool agvExists = await fixture.Repository.CheckIfAgvExistsAsync(agv.CorrelationId);
        agvExists.ShouldBeTrue();
    }

    [TestMethod]
    public async Task CheckIfAgvExistsAsync_NonExistingAgv_ReturnsFalse()
    {
        var fixture = new EfAgvRepositoryTestFixture();

        bool agvExists = await fixture.Repository.CheckIfAgvExistsAsync("".ToGuid());
        agvExists.ShouldBeFalse();
    }
}