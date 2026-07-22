using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Parsers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Mqtt;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Messages;

[TestClass]
public class FactsheetParserTest
{
    private readonly IMapper _mapper = MappingHelper.GetMapper(new AgvActionProfile());
    private readonly Mock<ILogger> _logger = new();

    private void ValidateAgvData(AgvData agvData, MqttFactsheet factsheet, Agv agv)
    {
        agvData.Model.ShouldBe(factsheet.TypeSpecification.SeriesName);
        agvData.Type.ShouldBe(agv.VehicleTypeId);
        agvData.IpV4Address.ShouldBe(factsheet.VehicleConfig?.Network?.LocalIpAddress);
        agvData.SoftwareVersion.ShouldBe(factsheet.VehicleConfig!.Versions!.First().Value);
        agvData.Acceleration.ShouldBe(factsheet.PhysicalParameters.AccelerationMax);
        agvData.MaxVelocityStraight.ShouldBe(factsheet.PhysicalParameters.SpeedMax);
        agvData.Width.ShouldBe(factsheet.PhysicalParameters.Width);
        agvData.Length.ShouldBe(factsheet.PhysicalParameters.Length);
        agvData.Payload.ShouldBe((long)factsheet.TypeSpecification.MaxLoadMass);
    }

    private void ValidateAgv(Agv agv, MqttFactsheet factsheet)
    {
        agv.SeriesName.ShouldBe(factsheet.TypeSpecification.SeriesName);
        agv.Manufacturer.ShouldBe(factsheet.Manufacturer);
        agv.CorrelationId.ShouldBe(factsheet.SerialNumber.ToGuid());
    }

    [TestMethod]
    public void Parse_MissingSerialNumber_ThrowsSerialNumberNotSetException()
    {
        var parser = new FactsheetParser(_logger.Object, _mapper);

        var factsheet = MqttFactsheetGenerator.Generate(string.Empty);

        Should.Throw<SerialNumberNotSetException>(() => parser.Parse(factsheet));
    }

    [TestMethod]
    public void Parse_ValidData_AllPropertiesParsed()
    {
        var parser = new FactsheetParser(_logger.Object, _mapper);

        var factsheet = MqttFactsheetGenerator.Generate();

        parser.Parse(factsheet);

        ValidateAgvData(parser.AgvData, factsheet, parser.Agv!);
        ValidateAgv(parser.Agv!, factsheet);
        parser.Actions.Count.ShouldBe(factsheet.ProtocolFeatures.AgvActions.Count);
    }
 }