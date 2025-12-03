using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.TransportOrder.Domain.Models.Filter;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class OwnedFilterRepositoryTest
{
    [TestMethod]
    public async Task GetOwnedFilters_ValidData_GetsFilters()
    {
        await using var fixture = await new OwnedFilterRepositoryTestFixture().BuildAsync();

        const string owner = "filter@email.com";

        await fixture.AddAsync(new OwnedFilter(Guid.NewGuid(), owner, new CombinationFilterDto([], LogicalCombinationVariant.And), "A"));
        await fixture.AddAsync(new OwnedFilter(Guid.NewGuid(), owner, new CombinationFilterDto([], LogicalCombinationVariant.Or), "A"));

        ICollection<OwnedFilter> result = await fixture.FilterRepository.GetResourcesForOwner(owner);

        result.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task AddOwnedFilter_ValidData_FilterAdded()
    {
        await using var fixture = await new OwnedFilterRepositoryTestFixture().BuildAsync();

        const string owner = "filter@email.com";

        var result = await fixture.FilterRepository.AddOwnedResource(new CombinationFilterDto([], LogicalCombinationVariant.And), owner, "aa");

        result.ShouldNotBe(Guid.Empty);
    }

    [TestMethod]
    public async Task UpdateOwnedFilter_ValidData_FilterUpdated()
    {
        await using var fixture = await new OwnedFilterRepositoryTestFixture().BuildAsync();

        const string owner = "filter@email.com";
        var filter = new OwnedFilter(Guid.NewGuid(), owner, new CombinationFilterDto([], LogicalCombinationVariant.And), "A");

        await fixture.AddAsync(filter);
        
        bool result = await fixture.FilterRepository.UpdateOwnedResource(new CombinationFilterDto([], LogicalCombinationVariant.Or), owner, filter.ResourceCorrelationId, "aa");

        result.ShouldBeTrue();

        ICollection<OwnedFilter> updated = await fixture.FilterRepository.GetResourcesForOwner(owner);

        updated.Count.ShouldBe(1);

        var updatedResource = updated.First().Resource as CombinationFilterDto;
        updatedResource.ShouldNotBeNull();
        updatedResource.Combination.ShouldBe(LogicalCombinationVariant.Or);
    }

    [TestMethod]
    public async Task UpdateOwnedFilter_NoFilterExists_Throws()
    {
        await using var fixture = await new OwnedFilterRepositoryTestFixture().BuildAsync();

        const string owner = "filter@email.com";

        await Should.ThrowAsync<OwnedResourceNotFoundException>(() => fixture.FilterRepository.UpdateOwnedResource(new CombinationFilterDto([], LogicalCombinationVariant.Or), owner, Guid.NewGuid(), "aa"));
    }

    [TestMethod]
    public async Task DeleteOwnedFilter_ValidData_FilterDeleted()
    {
        await using var fixture = await new OwnedFilterRepositoryTestFixture().BuildAsync();

        const string owner = "filter@email.com";
        var filter = new OwnedFilter(Guid.NewGuid(), owner, new CombinationFilterDto([], LogicalCombinationVariant.And), "A");

        await fixture.AddAsync(filter);

        bool result = await fixture.FilterRepository.DeleteOwnedResource(owner, filter.ResourceCorrelationId);

        result.ShouldBeTrue();

        ICollection<OwnedFilter> updated = await fixture.FilterRepository.GetResourcesForOwner(owner);

        updated.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task DeleteOwnedFilter_NoFilterExists_Throws()
    {
        await using var fixture = await new OwnedFilterRepositoryTestFixture().BuildAsync();

        const string owner = "filter@email.com";

        await Should.ThrowAsync<OwnedResourceNotFoundException>(() => fixture.FilterRepository.DeleteOwnedResource(owner, Guid.NewGuid()));
    }
}