using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Parsers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Messages;

[TestClass]
public class FactSheetParserTest
{
    private readonly IMapper _mapper = MappingHelper.GetMapper(new AgvActionProfile());
    private readonly Mock<ILogger> _logger = new();

    private void ValidateAgvData(AgvData agvData, MqttFactsheet factSheet)
    {
        agvData.Model.ShouldBe(factSheet.TypeSpecification.SeriesName);
        agvData.Type.ShouldBe(factSheet.TypeSpecification.AgvClass.ToString());
        agvData.IpV4Address.ShouldBe(factSheet.VehicleConfig?.Network?.LocalIpAddress);
        agvData.SoftwareVersion.ShouldBe(factSheet.VehicleConfig!.Versions!.First().Value);
        agvData.Acceleration.ShouldBe(factSheet.PhysicalParameters.AccelerationMax);
        agvData.MaxVelocityStraight.ShouldBe(factSheet.PhysicalParameters.SpeedMax);
        agvData.Width.ShouldBe(factSheet.PhysicalParameters.Width);
        agvData.Length.ShouldBe(factSheet.PhysicalParameters.Length);
        agvData.Payload.ShouldBe((long)factSheet.TypeSpecification.MaxLoadMass);
    }

    [TestMethod]
    public void Parse_MissingSerialNumber_ThrowsSerialNumberNotSetException()
    {
        var parser = new FactsheetParser(_logger.Object, _mapper);

        var factSheet = MqttFactSheetGenerator.Generate(serialNumber: string.Empty);

        Should.Throw<SerialNumberNotSetException>(() => parser.Parse(factSheet));
    }

    [TestMethod]
    public void Parse_ValidData_AllPropertiesParsed()
    {
        var parser = new FactsheetParser(_logger.Object, _mapper);

        var factSheet = MqttFactSheetGenerator.Generate();

        parser.Parse(factSheet);

        ValidateAgvData(parser.AgvData, factSheet);
        parser.Actions.Count.ShouldBe(factSheet.ProtocolFeatures.AgvActions.Count);
    }
}