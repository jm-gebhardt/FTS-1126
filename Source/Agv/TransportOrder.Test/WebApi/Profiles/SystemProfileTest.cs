using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Messages.Requests.System;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class SystemProfileTest
{
    [TestMethod]
    public void Map_ShouldMapTimeCorrectly_WhereGotSystemTimeIsMappedToDateTimeString()
    {
        var time = DateTimeOffset.Parse("2024-04-22T15:00:00+04:00").LocalDateTime;
        var gotSystemTime = new GotSystemTime(time);
        var mapper = MappingHelper.GetMapper(new SystemProfile());

        string dto = mapper.Map<string>(gotSystemTime);

        dto.ShouldBe("2024-04-22T11:00:00Z");
    }
}