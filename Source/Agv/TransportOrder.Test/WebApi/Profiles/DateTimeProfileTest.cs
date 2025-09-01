using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

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