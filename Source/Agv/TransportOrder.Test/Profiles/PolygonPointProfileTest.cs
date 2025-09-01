using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Profiles;

[TestClass]
public class PolygonPointProfileTest
{
    [TestInitialize]
    public void SetUp()
    {
        MappingHelper.GetMapper(new PolygonPointProfile());
    }

    private void AssertEquivalence<TDomain, TMessageContent, TPoint>(
        TDomain domainModel,
        TMessageContent messageContent,
        Func<TPoint, int> getIndex,
        Func<TPoint, double> getX,
        Func<TPoint, double> getY)
        where TDomain : class, ICollection<TPoint>
        where TMessageContent : class, ICollection<PolygonPoint>
    {
        domainModel.Count.ShouldBe(messageContent.Count);
        IEnumerable<(TPoint domain, PolygonPoint messageContent)> points = domainModel.Zip(messageContent);

        foreach (var point in points.Select((p, index) => new { DomainModel = p.domain, MessageContent = p.messageContent, Index = index }))
        {
            getIndex(point.DomainModel).ShouldBe(point.Index);
            getX(point.DomainModel).ShouldBe(point.MessageContent.X);
            getY(point.DomainModel).ShouldBe(point.MessageContent.Y);
        }
    }

    [TestMethod]
    public void Map_PolygonPointMessageList_MapsToPolygonPointDomainList()
    {
        var mapper = MappingHelper.GetMapper(new PolygonPointProfile());

        var polygonPointMessageContents = new List<PolygonPoint> { new(1, 2), new(2, 3) };

        var polygonPoints = mapper.Map<List<TransportOrder.Domain.Models.Polygon.PolygonPoint>>(polygonPointMessageContents);

        AssertEquivalence<ICollection<TransportOrder.Domain.Models.Polygon.PolygonPoint>, ICollection<PolygonPoint>, TransportOrder.Domain.Models.Polygon.PolygonPoint>(
            polygonPoints, polygonPointMessageContents,
            p => p.Index,
            p => p.X,
            p => p.Y
        );
    }

    [TestMethod]
    public void Map_PolygonPointDomainList_MapsToPolygonPointMessageList()
    {
        var mapper = MappingHelper.GetMapper(new PolygonPointProfile());

        var polygonPoints = new List<TransportOrder.Domain.Models.Polygon.PolygonPoint> { new(1, 2, 0, Guid.NewGuid()), new(2, 3, 1, Guid.NewGuid()) };

        var polygonPointMessageContents = mapper.Map<List<PolygonPoint>>(polygonPoints);

        AssertEquivalence<ICollection<TransportOrder.Domain.Models.Polygon.PolygonPoint>, ICollection<PolygonPoint>, TransportOrder.Domain.Models.Polygon.PolygonPoint>(
            polygonPoints, polygonPointMessageContents,
            p => p.Index,
            p => p.X,
            p => p.Y
        );
    }

    [TestMethod]
    public void Map_PolygonPointMessageICollection_MapsToPolygonPointDomainICollection()
    {
        var mapper = MappingHelper.GetMapper(new PolygonPointProfile());

        ICollection<PolygonPoint> polygonPointMessageContents = new List<PolygonPoint> { new(1, 2), new(2, 3) };

        var polygonPointsDomain = mapper.Map<ICollection<TransportOrder.Domain.Models.Polygon.PolygonPoint>>(polygonPointMessageContents);

        AssertEquivalence<ICollection<TransportOrder.Domain.Models.Polygon.PolygonPoint>, ICollection<PolygonPoint>, TransportOrder.Domain.Models.Polygon.PolygonPoint>(
            polygonPointsDomain, polygonPointMessageContents,
            p => p.Index,
            p => p.X,
            p => p.Y
        );
    }
}
