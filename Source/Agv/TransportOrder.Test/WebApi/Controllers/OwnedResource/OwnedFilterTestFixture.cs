using AutoMapper;
using Gebhardt.Acc.Common.Filter.Api.Dtos;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;
using Gebhardt.Acc.TransportOrder.Domain.Models.Filter;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.OwnedFilter;
using Gebhardt.Acc.TransportOrder.WebApi.Controllers.Filter;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.OwnedResource;

public class OwnedFilterTestFixture<TRequest> : ControllerTestFixture<TRequest, OwnedFilterRequestHandler, FilterController> where TRequest : class
{
    private readonly Mock<IOwnedFilterRepository> _filterRepo = new();

    public override OwnedFilterTestFixture<TRequest> Build()
    {
        base.Build();
        return this;
    }

    public OwnedFilterTestFixture()
    {
        AddServices(s =>
        {
            s.AddTransient(_ => _filterRepo.Object);
        });
    }

    protected override IEnumerable<Profile> GetProfiles() => [new OwnedFilterProfile()];

    protected override FilterController GetController()
    {
        return new FilterController(Mapper, GetRequestClient<GetOwnedFilters>(), GetRequestClient<AddOwnedFilter>(), GetRequestClient<UpdateOwnedFilter>(), GetRequestClient<DeleteOwnedFilter>());
    }

    public OwnedFilterTestFixture<TRequest> WithFilters(List<OwnedFilter> filters)
    {
        foreach (IGrouping<string, OwnedFilter> grouping in filters.GroupBy(of => of.Owner))
        {
            _filterRepo.Setup(r => r.GetResourcesForOwner(grouping.Key)).ReturnsAsync(grouping.ToList());
            foreach (var ownedFilter in grouping)
            {
                _filterRepo.Setup(r => r.UpdateOwnedResource(It.IsAny<FilterDto>(), ownedFilter.Owner, ownedFilter.ResourceCorrelationId, It.IsAny<string>())).ReturnsAsync(true);
                _filterRepo.Setup(r => r.DeleteOwnedResource(ownedFilter.Owner, ownedFilter.ResourceCorrelationId)).ReturnsAsync(true);
            }
        }
        
        _filterRepo.Setup(r => r.UpdateOwnedResource(It.IsAny<FilterDto>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>())).ReturnsAsync(true);
        _filterRepo.Setup(r => r.DeleteOwnedResource(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync(true);

        return this;
    }

    public OwnedFilterTestFixture<TRequest> WithAddableFilter(Guid addedFilterId)
    {
        _filterRepo.Setup(r => r.AddOwnedResource(It.IsAny<FilterDto>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(addedFilterId);
        return this;
    }
}