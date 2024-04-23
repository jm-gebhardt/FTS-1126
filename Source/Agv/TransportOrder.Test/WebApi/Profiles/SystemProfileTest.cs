using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.System;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.WebApi.Profiles;

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