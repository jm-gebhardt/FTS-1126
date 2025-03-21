using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using PolygonPoint = Gebhardt.Acc.TransportOrder.Domain.Models.Polygon.PolygonPoint;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class PolygonProfileTest
{
    private void AssertEquivalence<TDomain, TDto, TPoint>(
        TDomain domainModel,
        TDto dto,
        Func<TPoint, int> getIndex,
        Func<TPoint, double> getX,
        Func<TPoint, double> getY)
        where TDomain : class, ICollection<TPoint>
        where TDto : class, ICollection<PolygonPointDto>
    {
        domainModel.Count.ShouldBe(dto.Count);
        IEnumerable<(TPoint domain, PolygonPointDto dto)> points = domainModel.Zip(dto);

        foreach (var point in points.Select((p, index) => new { DomainModel = p.domain, Dto = p.dto, Index = index }))
        {
            getIndex(point.DomainModel).ShouldBe(point.Index);
            getX(point.DomainModel).ShouldBe(point.Dto.X);
            getY(point.DomainModel).ShouldBe(point.Dto.Y);
        }
    }

    [TestMethod]
    public void Map_PolygonPointDtoList_MapsToPolygonPointList()
    {
        var mapper = MappingHelper.GetMapper(new PolygonPointProfile());

        var polygonPointDtos = new List<PolygonPointDto> { new(1, 2), new(2, 3) };

        var polygonPoints = mapper.Map<List<PolygonPoint>>(polygonPointDtos);

        AssertEquivalence<ICollection<PolygonPoint>, ICollection<PolygonPointDto>, PolygonPoint>(
            polygonPoints, polygonPointDtos,
            p => p.Index,
            p => p.X,
            p => p.Y
        );
    }

    [TestMethod]
    public void Map_PolygonPointDtoICollection_MapsToPolygonPointICollection()
    {
        var mapper = MappingHelper.GetMapper(new PolygonPointProfile());

        ICollection<PolygonPointDto> polygonPointDtos = new List<PolygonPointDto> { new(1, 2), new(2, 3) };

        var polygonPoints = mapper.Map<ICollection<PolygonPoint>>(polygonPointDtos);

        AssertEquivalence<ICollection<PolygonPoint>, ICollection<PolygonPointDto>, PolygonPoint>(
            polygonPoints, polygonPointDtos,
            p => p.Index,
            p => p.X,
            p => p.Y
        );
    }

    [TestMethod]
    public void Map_PolygonPointDtoList_MapsToTriggerPolygonPointList()
    {
        var mapper = MappingHelper.GetMapper(new PolygonPointProfile());

        var polygonPointDtos = new List<PolygonPointDto> { new(1, 2), new(2, 3) };

        var polygonPoints = mapper.Map<List<TriggerPolygonPoint>>(polygonPointDtos);

        AssertEquivalence<ICollection<TriggerPolygonPoint>, ICollection<PolygonPointDto>, TriggerPolygonPoint>(
            polygonPoints, polygonPointDtos,
            p => p.Index,
            p => p.X,
            p => p.Y
        );
    }

    [TestMethod]
    public void Map_PolygonPointDtoICollection_MapsToTriggerPolygonPointICollection()
    {
        var mapper = MappingHelper.GetMapper(new PolygonPointProfile());

        ICollection<PolygonPointDto> polygonPointDtos = new List<PolygonPointDto> { new(1, 2), new(2, 3) };

        var polygonPoints = mapper.Map<ICollection<TriggerPolygonPoint>>(polygonPointDtos);

        AssertEquivalence<ICollection<TriggerPolygonPoint>, ICollection<PolygonPointDto>, TriggerPolygonPoint>(
            polygonPoints, polygonPointDtos,
            p => p.Index,
            p => p.X,
            p => p.Y
        );
    }

    [TestMethod]
    public void Map_PolygonPointDtoList_MapsToRestrictedAreaPolygonPointList()
    {
        var mapper = MappingHelper.GetMapper(new PolygonPointProfile());

        var polygonPointDtos = new List<PolygonPointDto> { new(1, 2), new(2, 3) };

        var polygonPoints = mapper.Map<List<RestrictedAreaPolygonPoint>>(polygonPointDtos);

        AssertEquivalence<ICollection<RestrictedAreaPolygonPoint>, ICollection<PolygonPointDto>, RestrictedAreaPolygonPoint>(
            polygonPoints, polygonPointDtos,
            p => p.Index,
            p => p.X,
            p => p.Y
        );
    }

    [TestMethod]
    public void Map_PolygonPointDtoICollection_MapsToRestrictedAreaPolygonPointICollection()
    {
        var mapper = MappingHelper.GetMapper(new PolygonPointProfile());

        ICollection<PolygonPointDto> polygonPointDtos = new List<PolygonPointDto> { new(1, 2), new(2, 3) };

        var polygonPoints = mapper.Map<ICollection<RestrictedAreaPolygonPoint>>(polygonPointDtos);

        AssertEquivalence<ICollection<RestrictedAreaPolygonPoint>, ICollection<PolygonPointDto>, RestrictedAreaPolygonPoint>(
            polygonPoints, polygonPointDtos,
            p => p.Index,
            p => p.X,
            p => p.Y
        );
    }

    [TestMethod]
    public void Map_PolygonPointDtoICollection_MapsToMessageTriggerPolygonPointICollection()
    {
        var mapper = MappingHelper.GetMapper(new PolygonPointProfile());

        ICollection<PolygonPointDto> polygonPointDtos = new List<PolygonPointDto> { new(1, 2), new(2, 3) };

        var polygonPoints = mapper.Map<ICollection<Common.Domain.Models.PolygonPoint>>(polygonPointDtos);

        polygonPoints.Count.ShouldBe(polygonPointDtos.Count);
    }
}