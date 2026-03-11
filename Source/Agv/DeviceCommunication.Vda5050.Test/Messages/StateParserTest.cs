using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Parsers;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Profiles;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Mqtt;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Messages;

[TestClass]
public class StateParserTest
{
    private readonly IMapper _mapper = MappingHelper.GetMapper(new CargoProfile(), new MqttErrorProfile(), new OrderProfile());
    private readonly Mock<ILogger> _logger = new();

    private static void ValidateAgvStatus(AgvStatus agvStatus, MqttState state)
    {
        agvStatus.CorrelationId.ShouldBe(state.SerialNumber.ToGuid());
        agvStatus.OrderId.ShouldBe(state.OrderId);
        agvStatus.BatteryCharge.ShouldBe(state.BatteryState.BatteryCharge);
        agvStatus.Charging.ShouldBe(state.BatteryState.Charging);
        agvStatus.BatteryVoltage.ShouldBe(state.BatteryState.BatteryVoltage ?? 0d);
    }

    private static void ValidateAgvPosition(AgvPosition agvPosition, MqttAgvPosition mqttPosition)
    {
        agvPosition.PositionInitialized.ShouldBe(mqttPosition.PositionInitialized);
        agvPosition.X.ShouldBe((float)mqttPosition.X);
        agvPosition.Y.ShouldBe((float)mqttPosition.Y);
        agvPosition.Theta.ShouldBe((float)mqttPosition.Theta);
        agvPosition.DeviationRange.ShouldBe((float?)mqttPosition.DeviationRange);
    }

    [TestMethod]
    public void Parse_MissingSerialNumber_ThrowsSerialNumberNotSetException()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate(serialNumber: string.Empty);

        Should.Throw<SerialNumberNotSetException>(() => parser.Parse(state));
    }

    [TestMethod]
    public void Parse_ValidData_AllPropertiesParsed()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate();

        parser.Parse(state);

        parser.SerialNumber.ShouldBe(state.SerialNumber);
        parser.CorrelationId.ShouldBe(state.SerialNumber.ToGuid());
        parser.Cargos.Count.ShouldBe(state.Loads!.Count);
        ValidateAgvStatus(parser.AgvStatus, state);
        parser.AgvErrors.ShouldNotBeNull();
    }

    [TestMethod]
    public void Parse_NullLoads_CargosIsEmpty()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate() with { Loads = null };

        parser.Parse(state);

        parser.Cargos.ShouldBeEmpty();
    }

    [TestMethod]
    public void Parse_LoadWithEmptyLoadId_ThrowsArgumentNullException()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate(cargo: [MqttStateGenerator.GenerateLoad(loadId: string.Empty)]);

        Should.Throw<ArgumentNullException>(() => parser.Parse(state));
    }

    [TestMethod]
    public void Parse_ErrorWithEmptyErrorType_ThrowsArgumentNullException()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate(errors: [MqttStateGenerator.GenerateError(errorType: string.Empty)]);

        Should.Throw<ArgumentNullException>(() => parser.Parse(state));
    }

    [TestMethod]
    public void Parse_NullPosition_AgvPositionIsNull()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate(position: null);

        parser.Parse(state);

        parser.AgvPosition.ShouldBeNull();
    }

    [TestMethod]
    public void Parse_PositionProvided_AgvPositionParsed()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var position = new MqttAgvPosition { X = 1.5, Y = 2.5, Theta = 0.5, PositionInitialized = true, DeviationRange = 0.1 };
        var state = MqttStateGenerator.Generate(position: position);

        parser.Parse(state);

        parser.AgvPosition.ShouldNotBeNull();
        ValidateAgvPosition(parser.AgvPosition, position);
    }

    [TestMethod]
    public void Parse_AutomaticOperatingMode_AgvStatusIsAutomatic()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate(); // Default is Automatic

        parser.Parse(state);

        parser.AgvStatus.Automatic.ShouldBeTrue();
    }

    [TestMethod]
    public void Parse_ManualOperatingMode_AgvStatusIsNotAutomatic()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate() with { OperatingMode = MqttOperatingMode.Manual };

        parser.Parse(state);

        parser.AgvStatus.Automatic.ShouldBeFalse();
    }

    [TestMethod]
    public void Parse_RunningActionState_AgvStatusIsBusy()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate(actionStates: [MqttStateGenerator.GenerateActionState(actionStatus: MqttActionStatus.Running)]);

        parser.Parse(state);

        parser.AgvStatus.Busy.ShouldBeTrue();
    }

    [TestMethod]
    public void Parse_InitializingActionState_AgvStatusIsBusy()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate(actionStates: [MqttStateGenerator.GenerateActionState(actionStatus: MqttActionStatus.Initializing)]);

        parser.Parse(state);

        parser.AgvStatus.Busy.ShouldBeTrue();
    }

    [TestMethod]
    public void Parse_WaitingActionState_AgvStatusIsNotBusy()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate(actionStates: [MqttStateGenerator.GenerateActionState(actionStatus: MqttActionStatus.Waiting)]);

        parser.Parse(state);

        parser.AgvStatus.Busy.ShouldBeFalse();
    }

    [TestMethod]
    public void Parse_NoActionStates_AgvStatusIsNotBusy()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate(actionStates: []);

        parser.Parse(state);

        parser.AgvStatus.Busy.ShouldBeFalse();
    }

    [TestMethod]
    public void Parse_EStopActive_AgvStatusIsDisturbed()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate() with { SafetyState = MqttStateGenerator.GenerateSafetyState(eStop: MqttEStop.Manual) };

        parser.Parse(state);

        parser.AgvStatus.Disturbed.ShouldBeTrue();
    }

    [TestMethod]
    public void Parse_FieldViolation_AgvStatusIsDisturbed()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate() with { SafetyState = MqttStateGenerator.GenerateSafetyState(fieldViolation: true) };

        parser.Parse(state);

        parser.AgvStatus.Disturbed.ShouldBeTrue();
    }

    [TestMethod]
    public void Parse_FieldViolation_AddsFieldViolationWarning()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var state = MqttStateGenerator.Generate() with { SafetyState = MqttStateGenerator.GenerateSafetyState(fieldViolation: true) };

        parser.Parse(state);

        parser.AgvErrors.ShouldNotBeNull();
        parser.AgvErrors.ShouldContain(e => e.IsFieldViolation());
    }

    [TestMethod]
    public void Parse_WithInformation_AddsInformationToErrors()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var info = MqttStateGenerator.GenerateInformation(infoType: "testInfo");
        var state = MqttStateGenerator.Generate(infos: [info]);

        parser.Parse(state);

        parser.AgvErrors.ShouldNotBeNull();
        parser.AgvErrors.ShouldContain(e => e.ErrorType == info.InfoType);
    }

    [TestMethod]
    public void Parse_NullInformation_NoInformationErrors()
    {
        var parser = new StateParser(_logger.Object, _mapper);
        var error = MqttStateGenerator.GenerateError();
        var state = MqttStateGenerator.Generate(errors: [error]) with { Information = null };

        parser.Parse(state);

        parser.AgvErrors.ShouldNotBeNull();
        parser.AgvErrors!.Count.ShouldBe(1);
    }
}