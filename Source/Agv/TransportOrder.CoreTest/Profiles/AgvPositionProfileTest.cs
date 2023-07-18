using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Profiles;

[TestClass]
public class AgvPositionProfileTest
{
    /// <summary>
    /// Maps the <see cref="AgvPositionChanged"/> to <see cref="AgvPositionStatus"/> and should map correctly
    /// </summary>
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvPositionChangedIsMappedToAgvPositionStatus()
    {
        string agvId = "agv1";
        var agvPositionChanged = new AgvPositionChanged(
            new AgvPosition(false, 0, 0, 0, 0),
            null,
            agvId,
            agvId.ToGuid()
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

        agvPositionStatus.AgvId.ShouldBe(agvPositionChanged.AgvId);
        agvPositionStatus.CorrelationId.ShouldBe(agvPositionChanged.CorrelationId);
    }

    /// <summary>
    /// Maps the <see cref="AgvPositionReceived"/> to <see cref="AgvPositionStatus"/> and should map correctly
    /// </summary>
    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvPositionReceivedIsMappedToAgvPositionStatus()
    {
        string agvId = "agv1";
        var agvPositionReceived = new AgvPositionReceived(
            agvId.ToGuid(),
            agvId,
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

        agvPositionStatus.AgvId.ShouldBe(agvPositionReceived.AgvId);
        agvPositionStatus.CorrelationId.ShouldBe(agvPositionReceived.CorrelationId);
    }

    [TestMethod]
    public void Map_ShouldMapAllPropertiesCorrectly_WhereAgvPositionStatusDtoIsMappedToAgvPositionStatus()
    {
        string agvId = "agv1";

        var agvPositionStatus = new AgvPositionStatus(
            Guid.NewGuid(),
            agvId,
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

        agvPositionStatusDto.AgvId.ShouldBe(agvPositionStatus.AgvId);
        agvPositionStatusDto.CorrelationId.ShouldBe(agvPositionStatus.CorrelationId);
    }
}