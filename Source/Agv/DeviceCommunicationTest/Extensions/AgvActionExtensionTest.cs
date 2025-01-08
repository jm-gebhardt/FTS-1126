using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System.Globalization;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.DeviceCommunicationTest.Extensions;

[TestClass]
public class AgvActionExtensionTest
{
    [TestMethod]
    public void GetDestinationStation_IsMoveToCoordinate_ThrowsInvalidOperationException()
    {
        var action = new AgvAction(Guid.NewGuid(), ActionType.Move, new ActionTarget(12, 12, 0), Guid.NewGuid());
        action.IsMoveToCoordinate.ShouldBeTrue();

        Should.Throw<InvalidOperationException>(action.GetDestinationStation);
    }

    [TestMethod]
    public void GetDestinationStation_Drop_ReturnsStationInfo()
    {
        const string toStationId = "Drop5";
        const double liftHeight = 1.23d;
        var action = new AgvAction(Guid.NewGuid(), ActionType.Drop, toStationId, Guid.NewGuid(), infoToAgv: new Dictionary<InfoToAgvKey, string>() { { InfoToAgvKey.LiftHeight, liftHeight.ToString(CultureInfo.InvariantCulture) } });

        var station = action.GetDestinationStation();

        station.ShouldNotBeNull();
        station.Id.ShouldBe(toStationId);
        station.LiftHeight.ShouldBe(liftHeight);
        station.DockFront.ShouldBeTrue();
    }

    [TestMethod]
    public void GetDestinationStation_Pick_ReturnsNull()
    {
        const string toStationId = "Pick1";
        const double liftHeight = 1.23d;
        var action = new AgvAction(Guid.NewGuid(), ActionType.Pick, toStationId, Guid.NewGuid(), infoToAgv: new Dictionary<InfoToAgvKey, string>() { { InfoToAgvKey.LiftHeight, liftHeight.ToString(CultureInfo.InvariantCulture) } });

        action.GetDestinationStation().ShouldBeNull();
    }

    [TestMethod]
    public void GetDestinationStation_Charge_ReturnsStationInfo()
    {
        const string toStationId = "Charge-1";
        var action = new AgvAction(Guid.NewGuid(), ActionType.Charge, toStationId, Guid.NewGuid());

        var station = action.GetDestinationStation();

        station.ShouldNotBeNull();
        station.Id.ShouldBe(toStationId);
        station.DockFront.ShouldBeTrue();
        station.LiftHeight.ShouldBeNull();
    }

    [TestMethod]
    public void GetDestinationStation_MoveToNode_ReturnsStationInfo()
    {
        const string toStationId = "Node-1";
        var action = new AgvAction(Guid.NewGuid(), ActionType.Move, toStationId, Guid.NewGuid())
        {
            InfoToAgv = new Dictionary<InfoToAgvKey, string> { { InfoToAgvKey.DockFront, "true" } }
        };

        var station = action.GetDestinationStation();

        station.ShouldNotBeNull();
        station.Id.ShouldBe(toStationId);
        station.DockFront.ShouldBeTrue();
        station.LiftHeight.ShouldBeNull();
    }

    [TestMethod]
    public void GetSourceStation_IsMoveToCoordinate_ThrowsInvalidOperationException()
    {
        var action = new AgvAction(Guid.NewGuid(), ActionType.Move, new ActionTarget(12, 12, 0), Guid.NewGuid());
        action.IsMoveToCoordinate.ShouldBeTrue();

        Should.Throw<InvalidOperationException>(action.GetSourceStation);
    }

    [DataRow(ActionType.Drop)]
    [DataRow(ActionType.Move)]
    [DataRow(ActionType.Charge)]
    [DataTestMethod]
    public void GetSourceStation_IsNotPick_ReturnsNull(ActionType actionType)
    {
        var action = new AgvAction(Guid.NewGuid(), actionType, "a node", Guid.NewGuid());
        action.GetSourceStation().ShouldBeNull();
    }

    [DataRow(OrderRecordType.OrderUpdate)]
    [DataRow(OrderRecordType.Order)]
    [DataTestMethod]
    public void ConvertToMoveToCoordinateOrder_ValidData_ReturnsMqttOrder(OrderRecordType recordType)
    {
        const string agvSerialNumber = "serialNumber";
        const string orderId = "orderId";
        var target = new ActionTarget(12, 12, 0);
        var action = new AgvAction(Guid.NewGuid(), ActionType.Move, target, Guid.NewGuid());
        action.IsMoveToCoordinate.ShouldBeTrue();

        var mqttOrder = action.ConvertToMoveToCoordinateOrder(orderId, recordType, agvSerialNumber);

        mqttOrder.SerialNumber.ShouldBe(agvSerialNumber);
        mqttOrder.Coordinates.ShouldBe(new Coordinates()
        {
            ToX = target.Position!.X,
            ToY = target.Position.Y,
            ToTheta = target.Position.Theta
        });
        mqttOrder.Stations.ShouldBeNull();
        mqttOrder.OrderType.ShouldBe(OrderType.Move);
        mqttOrder.RecordType.ShouldBe(recordType);
        mqttOrder.OrderId.ShouldBe(orderId);
    }

    [DataRow(ActionType.Pick)]
    [DataRow(ActionType.Drop)]
    [DataRow(ActionType.Move)]
    [DataRow(ActionType.Charge)]
    [DataTestMethod]
    public void ConvertToMoveToCoordinateOrder_NotMoveToCoordinate_ThrowsInvalidOperationException(ActionType actionType)
    {
        var action = new AgvAction(Guid.NewGuid(), actionType, "a node", Guid.NewGuid());
        action.IsMoveToCoordinate.ShouldBeFalse();

        Should.Throw<InvalidOperationException>(() => action.ConvertToMoveToCoordinateOrder("orderId", OrderRecordType.Order, "agv serial number"));
    }

    [DataRow(OrderRecordType.OrderUpdate)]
    [DataRow(OrderRecordType.Order)]
    [DataTestMethod]
    public void ConvertToMoveToStationsOrder_ValidData_ReturnsMqttOrder(OrderRecordType recordType)
    {
        var actionCorrelationId = Guid.NewGuid();
        const string agvSerialNumber = "agv0";
        const string orderId = "1234";
        var orderCorrelationId = orderId.ToGuid();
        const string toStationId = "Drop5";
        const double liftHeight = 1.23d;
        var action = new AgvAction(actionCorrelationId, ActionType.Drop, toStationId, orderCorrelationId, infoToAgv: new Dictionary<InfoToAgvKey, string>() { { InfoToAgvKey.LiftHeight, liftHeight.ToString(CultureInfo.InvariantCulture) } });

        var mqttOrder = action.ConvertToMoveToStationsOrder(orderId, recordType, agvSerialNumber, [], []);

        mqttOrder.SerialNumber.ShouldBe(agvSerialNumber);
        mqttOrder.OrderType.ShouldBe(OrderType.Drop);
        mqttOrder.RecordType.ShouldBe(recordType);
        mqttOrder.Stations?.ToStation?.Id.ShouldBe(toStationId);
        mqttOrder.Stations?.ToStation?.DockFront.ShouldBeTrue();
        mqttOrder.Stations?.ToStation?.LiftHeight.ShouldBe(liftHeight);
        mqttOrder.Stations?.FromStation.ShouldBeNull();
        mqttOrder.OrderId.ShouldBe(orderId);
    }

    [TestMethod]
    public void ConvertToMoveToStationsOrder_IsMoveToCoordinate_ThrowsInvalidOperationException()
    {
        var action = new AgvAction(Guid.NewGuid(), ActionType.Move, new ActionTarget(12, 12, 0), Guid.NewGuid());
        action.IsMoveToCoordinate.ShouldBeTrue();

        Should.Throw<InvalidOperationException>(() => action.ConvertToMoveToStationsOrder("orderId", OrderRecordType.Order, "agv serial number", [], []));
    }

    [DataRow(OrderRecordType.OrderUpdate)]
    [DataRow(OrderRecordType.Order)]
    [DataTestMethod]
    public void ConvertToOrder_IsMoveToCoordinate_ReturnsMqttOrder(OrderRecordType recordType)
    {
        const string agvSerialNumber = "serialNumber";
        const string orderId = "orderId";
        var target = new ActionTarget(12, 12, 0);
        var action = new AgvAction(Guid.NewGuid(), ActionType.Move, target, Guid.NewGuid());
        action.IsMoveToCoordinate.ShouldBeTrue();

        var mqttOrder = action.ConvertToOrder(orderId, recordType, agvSerialNumber, [], []);

        mqttOrder.SerialNumber.ShouldBe(agvSerialNumber);
        mqttOrder.Coordinates.ShouldBe(new Coordinates()
        {
            ToX = target.Position!.X,
            ToY = target.Position.Y,
            ToTheta = target.Position.Theta
        });
        mqttOrder.Stations.ShouldBeNull();
        mqttOrder.OrderType.ShouldBe(OrderType.Move);
        mqttOrder.RecordType.ShouldBe(recordType);

        mqttOrder.OrderId.ShouldBe(orderId);
    }

    [DataRow(OrderRecordType.OrderUpdate)]
    [DataRow(OrderRecordType.Order)]
    [DataTestMethod]
    public void ConvertToOrder_IsNotMoveToCoordinate_ReturnsMqttOrder(OrderRecordType recordType)
    {
        var actionCorrelationId = Guid.NewGuid();
        const string agvSerialNumber = "agv0";
        const string orderId = "1234";
        var orderCorrelationId = orderId.ToGuid();
        const string toStationId = "Drop5";
        const double liftHeight = 1.23d;
        var action = new AgvAction(actionCorrelationId, ActionType.Drop, toStationId, orderCorrelationId, infoToAgv: new Dictionary<InfoToAgvKey, string>() { { InfoToAgvKey.LiftHeight, liftHeight.ToString(CultureInfo.InvariantCulture) } });

        var mqttOrder = action.ConvertToOrder(orderId, recordType, agvSerialNumber, [], []);

        mqttOrder.SerialNumber.ShouldBe(agvSerialNumber);
        mqttOrder.OrderType.ShouldBe(OrderType.Drop);
        mqttOrder.RecordType.ShouldBe(recordType);
        mqttOrder.Stations?.ToStation?.Id.ShouldBe(toStationId);
        mqttOrder.Stations?.ToStation?.DockFront.ShouldBeTrue();
        mqttOrder.Stations?.ToStation?.LiftHeight.ShouldBe(liftHeight);
        mqttOrder.Stations?.FromStation.ShouldBeNull();
        mqttOrder.OrderId.ShouldBe(orderId);
    }
}