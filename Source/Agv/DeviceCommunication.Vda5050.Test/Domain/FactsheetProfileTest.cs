using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvTypes;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Factsheets;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Profiles;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain;

[TestClass]
public class FactsheetProfileTest
{
    private IMapper _mapper = null!;

    [TestInitialize]
    public void SetUp()
    {
        _mapper = MappingHelper.GetMapper(new FactsheetProfile());
    }

    [TestMethod]
    public void Map_FactsheetToAgvData_MapsAllProperties()
    {
        var factsheet = new Factsheet
        {
            SerialNumber = "SN-001",
            Manufacturer = "Gebhardt",
            TypeSpecification = new TypeSpecification { SeriesName = "GTL", MaxLoadMass = 1500 },
            PhysicalParameters = new PhysicalParameters
            {
                SpeedMax = 1.5,
                AccelerationMax = 0.8,
                Width = 0.8,
                Length = 1.2
            },
            VehicleConfig = new VehicleConfig
            {
                Versions = [new VehicleVersion { Key = "softwareVersion", Value = "1.2.3" }],
                Network = new NetworkConfig { LocalIpAddress = "192.168.1.100" }
            }
        };

        var agvData = _mapper.Map<AgvData>(factsheet);

        agvData.AgvCorrelationId.ShouldBe("SN-001".ToGuid());
        agvData.Model.ShouldBe("GTL");
        agvData.Type.ShouldBe("Gebhardt.GTL");
        agvData.IpV4Address.ShouldBe("192.168.1.100");
        agvData.SoftwareVersion.ShouldBe("1.2.3");
        agvData.MaxVelocityStraight.ShouldBe(1.5);
        agvData.Acceleration.ShouldBe(0.8);
        agvData.Width.ShouldBe(0.8);
        agvData.Length.ShouldBe(1.2);
        agvData.Payload.ShouldBe(1500L);
        agvData.MaxVelocityTurnOnSpot.ShouldBe(0.0);
        agvData.Sensors.ShouldBeEmpty();
    }

    [TestMethod]
    public void Map_FactsheetWithoutVehicleConfig_MapsIpAndSoftwareVersionAsEmpty()
    {
        var factsheet = new Factsheet
        {
            SerialNumber = "SN-002",
            TypeSpecification = new TypeSpecification(),
            PhysicalParameters = new PhysicalParameters()
        };

        var agvData = _mapper.Map<AgvData>(factsheet);

        agvData.IpV4Address.ShouldBe(string.Empty);
        agvData.SoftwareVersion.ShouldBe(string.Empty);
    }

    [TestMethod]
    public void Map_FactsheetToAgvType_MarksAgvTypeAsNotReportedByAgv()
    {
        const string manufacturer = "Gebhardt";
        const string seriesName = "GTL";
        var factsheet = new Factsheet
        {
            SerialNumber = "SN-003",
            Manufacturer = manufacturer,
            TypeSpecification = new TypeSpecification { SeriesName = seriesName },
            PhysicalParameters = new PhysicalParameters { Width = 0.8, Length = 1.2 }
        };

        var agvType = _mapper.Map<AgvType>(factsheet);

        agvType.AgvTypeIdentifier.ShouldBe($"{manufacturer}.{seriesName}");
        agvType.ReportedByAgv.ShouldBeFalse();
        agvType.Acknowledged.ShouldBeTrue();
    }

    [TestMethod]
    public void Map_AgvTypeToAgvTypeInfo_CarriesReportedByAgv()
    {
        const string manufacturer = "Gebhardt";
        const string seriesName = "GTL";
        var agvType = new AgvType($"{manufacturer}.{seriesName}", manufacturer, seriesName, 1.2, 0.8, string.Empty, acknowledged: false, reportedByAgv: true);

        var agvTypeInfo = _mapper.Map<AgvTypeInfo>(agvType);

        agvTypeInfo.ReportedByAgv.ShouldBeTrue();
        agvTypeInfo.Acknowledged.ShouldBeFalse();
    }

    [TestMethod]
    public void Map_FactsheetActionToAgvAction_MapsAllProperties()
    {
        var factsheetAction = new FactsheetAction
        {
            ActionType = "pick",
            ActionDescription = "Pick action",
            ActionScopes = [ActionScope.Node, ActionScope.Edge],
            ResultDescription = "Item picked",
            BlockingTypes = [BlockingType.HARD],
            ActionParameters =
            [
                new FactsheetActionParameter
                {
                    Key = "height",
                    ValueDataType = ValueDataType.Float,
                    Description = "Lift height in metres",
                    IsOptional = false
                }
            ]
        };

        var agvAction = _mapper.Map<AgvAction>(factsheetAction);

        agvAction.ActionType.ShouldBe("pick");
        agvAction.ActionDescription.ShouldBe("Pick action");
        agvAction.ActionScopes.ShouldBe([ActionScope.Node, ActionScope.Edge]);
        agvAction.ResultDescription.ShouldBe("Item picked");
        agvAction.BlockingTypes.ShouldBe([BlockingType.HARD]);
        var parameter = agvAction.ActionParameters!.Single();
        parameter.Key.ShouldBe("height");
        parameter.ValueDataType.ShouldBe(ValueDataType.Float);
        parameter.Description.ShouldBe("Lift height in metres");
        parameter.IsOptional.ShouldBeFalse();
    }

    [TestMethod]
    public void Map_FactsheetActionParameterWithNullIsOptional_MapsIsOptionalAsFalse()
    {
        var parameter = new FactsheetActionParameter
        {
            Key = "angle",
            ValueDataType = ValueDataType.Float,
            IsOptional = null
        };

        var agvParameter = _mapper.Map<AgvActionParameter>(parameter);

        agvParameter.IsOptional.ShouldBeFalse();
    }
}