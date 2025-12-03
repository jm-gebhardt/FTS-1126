using System.Security.Claims;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.TransportOrder.Domain.Models.Filter;
using Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Filter;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.OwnedResource;

[TestClass]
public class OwnedFilterControllerTest
{
    [TestMethod]
    public async Task GetFilters_ValidData_GetsFiltersForOwner()
    {
        OwnedFilterTestFixture<GetOwnedFilters> fixture = new OwnedFilterTestFixture<GetOwnedFilters>()
            .WithClaim<OwnedFilterTestFixture<GetOwnedFilters>>(new Claim(ClaimTypes.Email, "test@email.com"))
            .WithFilters([
                new OwnedFilter(Guid.NewGuid(), "test@email.com", new CombinationFilterDto([], LogicalCombinationVariant.And), "A"),
                new OwnedFilter(Guid.NewGuid(), "test@email.com", new CombinationFilterDto([], LogicalCombinationVariant.And), "A"),
                new OwnedFilter(Guid.NewGuid(), "test2@email.com", new CombinationFilterDto([], LogicalCombinationVariant.And), "A"),
                new OwnedFilter(Guid.NewGuid(), "test3@email.com", new CombinationFilterDto([], LogicalCombinationVariant.And), "A")
            ]).Build();

        ICollection<OwnedFilterDto> result = await fixture.GetControllerResultAsync(c => c.GetFilters());

        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
    }

    [TestMethod]
    public async Task AddFilter_ValidData_AddsFilter()
    {
        var addedFilterId = Guid.NewGuid();

        OwnedFilterTestFixture<AddOwnedFilter> fixture = new OwnedFilterTestFixture<AddOwnedFilter>()
            .WithClaim<OwnedFilterTestFixture<AddOwnedFilter>>(new Claim(ClaimTypes.Email, "test@email.com"))
            .WithAddableFilter(addedFilterId).Build();

        var result = await fixture.GetControllerResultValueAsync(c => c.AddFilter(new CombinationFilterDto([], LogicalCombinationVariant.And), "A"));

        result.ShouldBe(addedFilterId);
    }

    [TestMethod]
    public async Task UpdateFilter_ValidData_UpdatesFiltersForOwner()
    {
        var filterToUpdate = Guid.NewGuid();

        OwnedFilterTestFixture<UpdateOwnedFilter> fixture = new OwnedFilterTestFixture<UpdateOwnedFilter>()
            .WithClaim<OwnedFilterTestFixture<UpdateOwnedFilter>>(new Claim(ClaimTypes.Email, "test@email.com"))
            .WithFilters([
                new OwnedFilter(filterToUpdate, "test@email.com", new CombinationFilterDto([], LogicalCombinationVariant.And), "A"),
                new OwnedFilter(Guid.NewGuid(), "test@email.com", new CombinationFilterDto([], LogicalCombinationVariant.And), "A"),
                new OwnedFilter(Guid.NewGuid(), "test2@email.com", new CombinationFilterDto([], LogicalCombinationVariant.And), "A"),
                new OwnedFilter(Guid.NewGuid(), "test3@email.com", new CombinationFilterDto([], LogicalCombinationVariant.And), "A")
            ]).Build();

        bool result = await fixture.GetControllerResultValueAsync(c => c.UpdateFilter(filterToUpdate, new CombinationFilterDto([], LogicalCombinationVariant.Or), "aa"));

        result.ShouldBeTrue();
    }

    [TestMethod]
    public async Task DeleteFilter_ValidData_DeletesFiltersForOwner()
    {
        var filterToDelete = Guid.NewGuid();

        OwnedFilterTestFixture<DeleteOwnedFilter> fixture = new OwnedFilterTestFixture<DeleteOwnedFilter>()
            .WithClaim<OwnedFilterTestFixture<DeleteOwnedFilter>>(new Claim(ClaimTypes.Email, "test@email.com"))
            .WithFilters([
                new OwnedFilter(filterToDelete, "test@email.com", new CombinationFilterDto([], LogicalCombinationVariant.And), "A"),
                new OwnedFilter(Guid.NewGuid(), "test@email.com", new CombinationFilterDto([], LogicalCombinationVariant.And), "A"),
                new OwnedFilter(Guid.NewGuid(), "test2@email.com", new CombinationFilterDto([], LogicalCombinationVariant.And), "A"),
                new OwnedFilter(Guid.NewGuid(), "test3@email.com", new CombinationFilterDto([], LogicalCombinationVariant.And), "A")
            ]).Build();

        bool result = await fixture.GetControllerResultValueAsync(c => c.DeleteFilter(filterToDelete));

        result.ShouldBeTrue();
    }
}