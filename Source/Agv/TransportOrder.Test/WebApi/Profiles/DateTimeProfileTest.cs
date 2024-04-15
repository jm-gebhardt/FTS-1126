using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class DateTimeProfileTest
{
    [TestMethod]
    public void Map_KindUtc_ReturnsKindUtc()
    {
        var now = DateTime.UtcNow;
        var mapper = MappingHelper.GetMapper(new DateTimeProfile());
        var mappedDateTime = mapper.Map<DateTime>(now);
        mappedDateTime.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [TestMethod]
    public void Map_KindUnspecified_ReturnsKindUtc()
    {
        var now = DateTime.UtcNow;
        now = DateTime.SpecifyKind(now, DateTimeKind.Unspecified);
        var mapper = MappingHelper.GetMapper(new DateTimeProfile());
        var mappedDateTime = mapper.Map<DateTime>(now);
        mappedDateTime.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [TestMethod]
    public void Map_KindLocal_ReturnsKindUtc()
    {
        var now = DateTime.Now;
        now = DateTime.SpecifyKind(now, DateTimeKind.Local);
        var mapper = MappingHelper.GetMapper(new DateTimeProfile());
        var mappedDateTime = mapper.Map<DateTime>(now);
        mappedDateTime.Kind.ShouldBe(DateTimeKind.Utc);
    }
}