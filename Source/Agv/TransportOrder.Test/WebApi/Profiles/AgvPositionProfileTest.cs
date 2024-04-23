using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using Gebhardt.Acc.TransportOrder.Messages.MessageContent;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class AgvPositionProfileTest
{
    /// <summary>
    /// Maps the <see cref="AgvPositionChanged"/> to <see cref="AgvPositionStatus"/> and should map correctly
    /// </summary>
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvPositionChangedIsMappedToAgvPositionStatus()
    {
        const string agvSerialNumber = "agv1";
        var agvPositionChanged = new AgvPositionChanged(
            agvSerialNumber.ToGuid(),
            new AgvPosition(false, 0, 0, 0, 0),
            null,
            agvSerialNumber
        );

        var mapper = MappingHelper.GetMapper(new AgvPositionProfile());
        var agvPositionStatus = mapper.Map<AgvPositionStatus>(agvPositionChanged);

        agvPositionStatus.ShouldNotBeNull();
        agvPositionStatus.AgvPosition.ShouldSatisfyAllConditions(s => s.PositionInitialized.ShouldBe(agvPositionChanged.NewAgvPosition.PositionInitialized),
            s => s.X.ShouldBe(agvPositionChanged.NewAgvPosition.X),
            s => s.Y.ShouldBe(agvPositionChanged.NewAgvPosition.Y),
            s => s.Z.ShouldBe(agvPositionChanged.NewAgvPosition.Z),
            s => s.Theta.ShouldBe(agvPositionChanged.NewAgvPosition.Theta),
            s => s.DeviationRange.ShouldBe(agvPositionChanged.NewAgvPosition.DeviationRange));

        agvPositionStatus.SerialNumber.ShouldBe(agvPositionChanged.SerialNumber);
        agvPositionStatus.CorrelationId.ShouldBe(agvPositionChanged.CorrelationId);
    }

    /// <summary>
    /// Maps the <see cref="AgvPositionReceived"/> to <see cref="AgvPositionStatus"/> and should map correctly
    /// </summary>
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvPositionReceivedIsMappedToAgvPositionStatus()
    {
        const string agvSerialNumber = "agv1";
        var agvPositionReceived = new AgvPositionReceived(
            agvSerialNumber.ToGuid(),
            agvSerialNumber,
            new AgvPosition(false, 0, 0, 0, 0)
        );

        var mapper = MappingHelper.GetMapper(new AgvPositionProfile());
        var agvPositionStatus = mapper.Map<AgvPositionStatus>(agvPositionReceived);

        agvPositionStatus.ShouldNotBeNull();
        agvPositionStatus.AgvPosition.ShouldSatisfyAllConditions(s => s.PositionInitialized.ShouldBe(agvPositionReceived.AgvPosition.PositionInitialized),
            s => s.X.ShouldBe(agvPositionReceived.AgvPosition.X),
            s => s.Y.ShouldBe(agvPositionReceived.AgvPosition.Y),
            s => s.Z.ShouldBe(agvPositionReceived.AgvPosition.Z),
            s => s.Theta.ShouldBe(agvPositionReceived.AgvPosition.Theta),
            s => s.DeviationRange.ShouldBe(agvPositionReceived.AgvPosition.DeviationRange));

        agvPositionStatus.SerialNumber.ShouldBe(agvPositionReceived.SerialNumber);
        agvPositionStatus.CorrelationId.ShouldBe(agvPositionReceived.CorrelationId);
    }

    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvPositionStatusDtoIsMappedToAgvPositionStatus()
    {
        const string agvSerialNumber = "agv1";

        var agvPositionStatus = new AgvPositionStatus(
            Guid.NewGuid(),
            agvSerialNumber,
            new AgvPosition(false, 0, 0, 0, 0)
        );

        var mapper = MappingHelper.GetMapper(new AgvPositionProfile());
        var agvPositionStatusDto = mapper.Map<AgvPositionStatusDto>(agvPositionStatus);

        agvPositionStatusDto.ShouldNotBeNull();
        agvPositionStatusDto.AgvPosition.ShouldSatisfyAllConditions(
            s => s.PositionInitialized.ShouldBe(agvPositionStatus.AgvPosition.PositionInitialized),
            s => s.X.ShouldBe(agvPositionStatus.AgvPosition.X),
            s => s.Y.ShouldBe(agvPositionStatus.AgvPosition.Y),
            s => s.Z.ShouldBe(agvPositionStatus.AgvPosition.Z),
            s => s.Theta.ShouldBe(agvPositionStatus.AgvPosition.Theta),
            s => s.DeviationRange.ShouldBe(agvPositionStatus.AgvPosition.DeviationRange));

        agvPositionStatusDto.SerialNumber.ShouldBe(agvPositionStatus.SerialNumber);
        agvPositionStatusDto.CorrelationId.ShouldBe(agvPositionStatus.CorrelationId);
    }
}