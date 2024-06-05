using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Exceptions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.MessageParsers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Profiles;
using Gebhardt.Acc.DeviceCommunicationTest.Helper;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Parsers;

[TestClass]
public class StateParserTest
{
    private readonly IMapper _mapper = MappingHelper.GetMapper([new CargoProfile(), new MqttErrorProfile()]);
    private readonly Mock<ILogger> _logger = new();

    private void ValidatePosition(MqttState state, AgvPosition position)
    {
        state.AgvPosition.ShouldNotBeNull();
        position.X.ShouldBe(state.AgvPosition.X);
        position.Y.ShouldBe(state.AgvPosition.Y * -1);
        position.Z.ShouldBe(0);
        position.Theta.ShouldBe((float)Mathematics.ConvertToRadians(90.0f) - state.AgvPosition.Theta);
        position.DeviationRange.ShouldBe(state.AgvPosition.DeviationRange);
        position.PositionInitialized.ShouldBe(state.AgvPosition.PositionInitialized);
    }

    private void ValidDataAgvStatus(MqttState state, AgvStatus agvStatus)
    {
        agvStatus.CorrelationId.ShouldBe(state.SerialNumber.ToGuid());
        agvStatus.OrderId.ShouldBe(state.OrderId);
    }

    [TestMethod]
    public void Parse_ValidData_PropertiesSet()
    {
        var mqttState = MqttStateGenerator.GenerateState("Agv-01", "order-01");
        var stateParser = new StateParser(_logger.Object, _mapper);

        stateParser.Parse(mqttState);

        stateParser.SerialNumber.ShouldBe(mqttState.SerialNumber);
        stateParser.CorrelationId.ShouldBe(mqttState.SerialNumber.ToGuid());
        stateParser.AgvErrors.ShouldBeNull();
        stateParser.Cargos.ShouldBeEmpty();
        stateParser.AgvStatus.Disturbed.ShouldBeFalse();
        ValidatePosition(mqttState, stateParser.AgvPosition);
        ValidDataAgvStatus(mqttState, stateParser.AgvStatus);
    }

    [TestMethod]
    public void Parse_EmptySerialNumber_ThrowsSerialNumberNotSetException()
    {
        var mqttState = MqttStateGenerator.GenerateState(string.Empty, "order-01");
        var stateParser = new StateParser(_logger.Object, _mapper);

        Should.Throw<SerialNumberNotSetException>(() => stateParser.Parse(mqttState));
    }

    [TestMethod]
    public void Parse_LoadIdNotSet_ThrowsArgumentNullException()
    {
        var mqttState = MqttStateGenerator.GenerateState("Agv-01", "order-01", loads: [new MqttLoad { LoadId = null }]);
        var stateParser = new StateParser(_logger.Object, _mapper);

        Should.Throw<ArgumentNullException>(() => stateParser.Parse(mqttState));
    }

    [TestMethod]
    public void Parse_ErrorTypeNotSet_ThrowsArgumentNullException()
    {
        var mqttState = MqttStateGenerator.GenerateState("Agv-01", "order-01", errors: [new MqttError { ErrorType = null }]);
        var stateParser = new StateParser(_logger.Object, _mapper);

        Should.Throw<ArgumentNullException>(() => stateParser.Parse(mqttState));
    }

    [TestMethod]
    public void Parse_WithErrors_AgvErrorsSet()
    {
        var mqttError = new MqttError { ErrorType = "a type", ErrorLevel = MqttErrorLevel.Fatal, ErrorDescription = "error description" };
        var mqttState = MqttStateGenerator.GenerateState("Agv-01", "order-01", errors: [mqttError]);
        var stateParser = new StateParser(_logger.Object, _mapper);

        stateParser.Parse(mqttState);

        stateParser.AgvStatus.Disturbed.ShouldBeFalse();
        stateParser.AgvErrors.ShouldNotBeNull();
        stateParser.AgvErrors.Single().AgvCorrelationId.ShouldBe(mqttState.SerialNumber.ToGuid());
        stateParser.AgvErrors.Single().ErrorLevel.ShouldBe(ErrorLevel.Fatal);
        stateParser.AgvErrors.Single().ErrorType.ShouldBe(mqttError.ErrorType);
    }

    [TestMethod]
    public void Parse_WithLoad_AgvCargosSet()
    {
        var mqttLoad = new MqttLoad() { LoadId = "loadId" };
        var mqttState = MqttStateGenerator.GenerateState("Agv-01", "order-01", loads: [mqttLoad]);
        var stateParser = new StateParser(_logger.Object, _mapper);

        stateParser.Parse(mqttState);

        stateParser.AgvStatus.Disturbed.ShouldBeFalse();
        stateParser.Cargos.ShouldNotBeEmpty();
        stateParser.Cargos.Single().LoadId.ShouldBe(mqttLoad.LoadId);
    }

    [TestMethod]
    public void Parse_WithFieldViolation_WarningAdded()
    {
        var mqttError = new MqttError { ErrorType = "a type", ErrorLevel = MqttErrorLevel.Fatal, ErrorDescription = "error description" };
        var mqttState = MqttStateGenerator.GenerateState("Agv-01", "order-01", errors: [mqttError], safetyState: new MqttSafetyState {FieldViolation = true});
        var stateParser = new StateParser(_logger.Object, _mapper);

        stateParser.Parse(mqttState);
        
        stateParser.AgvErrors.ShouldNotBeNull();
        stateParser.AgvErrors.Count.ShouldBe(2);
        stateParser.AgvStatus.Disturbed.ShouldBeTrue();
        var fieldViolationWarning = stateParser.AgvErrors.Single(e => e is FieldViolation);
        fieldViolationWarning.ErrorLevel.ShouldBe(ErrorLevel.Warning);
    }

    [TestMethod]
    public void Parse_WithFieldViolationNoOtherErrors_WarningAdded()
    {
        var mqttState = MqttStateGenerator.GenerateState("Agv-01", "order-01", safetyState: new MqttSafetyState { FieldViolation = true });
        var stateParser = new StateParser(_logger.Object, _mapper);

        stateParser.Parse(mqttState);

        stateParser.AgvErrors.ShouldNotBeNull();
        stateParser.AgvErrors.Count.ShouldBe(1);
        stateParser.AgvStatus.Disturbed.ShouldBeTrue();
        var fieldViolationWarning = stateParser.AgvErrors.Single(e => e is FieldViolation);
        fieldViolationWarning.ErrorLevel.ShouldBe(ErrorLevel.Warning);
    }

    [TestMethod]
    public void Parse_WithFieldViolationEmptyOtherErrors_WarningAdded()
    {
        var mqttState = MqttStateGenerator.GenerateState("Agv-01", "order-01", errors: [], safetyState: new MqttSafetyState { FieldViolation = true });
        var stateParser = new StateParser(_logger.Object, _mapper);

        stateParser.Parse(mqttState);

        stateParser.AgvErrors.ShouldNotBeNull();
        stateParser.AgvErrors.Count.ShouldBe(1);
        stateParser.AgvStatus.Disturbed.ShouldBeTrue();
        var fieldViolationWarning = stateParser.AgvErrors.Single(e => e is FieldViolation);
        fieldViolationWarning.ErrorLevel.ShouldBe(ErrorLevel.Warning);
    }

    [TestMethod]
    public void Parse_WithNonFieldViolationSafetyState_NoWarningAdded()
    {
        var mqttError = new MqttError { ErrorType = "a type", ErrorLevel = MqttErrorLevel.Fatal, ErrorDescription = "error description" };
        var mqttState = MqttStateGenerator.GenerateState("Agv-01", "order-01", errors: [mqttError], safetyState: new MqttSafetyState { FieldViolation = false, EStop = new MqttEStop()});
        var stateParser = new StateParser(_logger.Object, _mapper);

        stateParser.Parse(mqttState);

        stateParser.AgvErrors.ShouldNotBeNull();
        stateParser.AgvStatus.Disturbed.ShouldBeTrue();
        stateParser.AgvErrors.Count.ShouldBe(1);
        stateParser.AgvErrors.SingleOrDefault(e => e is FieldViolation).ShouldBeNull();
    }
}