using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Messages.MqttMessages;
using Gebhardt.Acc.DeviceCommunication.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Profiles;

[TestClass]
public class CargoProfileTest
{
    private IMapper _mapper = null!;

    [TestInitialize]
    public void SetUp()
    {
        _mapper = MappingHelper.GetMapper(new CargoProfile());
    }

    [TestMethod]
    public void Map_ShouldMapMqttLoadToCargoCorrectly()
    {
        var mqttLoad = new MqttLoad
        {
            BoundingBoxReference = new MqttBoundingBoxReference { X = 1, Y = 2, Z = 3, Theta = 0.5f },
            LoadDimensions = new MqttLoadDimensions { Width = 2, Length = 3, Height = 4 },
            LoadId = "Load123",
            LoadPosition = "front",
            LoadType = "TypeA",
            Weight = 5.5f
        };

        var cargo = _mapper.Map<Cargo>(mqttLoad);

        cargo.ShouldSatisfyAllConditions(
            p => p.ShouldNotBeNull(),
            p => p.LoadId.ShouldBe(mqttLoad.LoadId),
            p => p.LoadType.ShouldBe(mqttLoad.LoadType),
            p => p.LoadPosition.ShouldBe(mqttLoad.LoadPosition),
            p => p.Weight.ShouldBe(mqttLoad.Weight),
            p => p.CenterPointPosition.ShouldNotBeNull(),
            p => p.CenterPointPosition!.X.ShouldBe(mqttLoad.BoundingBoxReference.X),
            p => p.CenterPointPosition!.Y.ShouldBe(mqttLoad.BoundingBoxReference.Y),
            p => p.CenterPointPosition!.Z.ShouldBe(mqttLoad.BoundingBoxReference.Z),
            p => ((float?)p.CenterPointPosition!.Theta).ShouldBe(mqttLoad.BoundingBoxReference.Theta),
            p => p.Dimension.ShouldNotBeNull(),
            p => p.Dimension!.Width.ShouldBe(mqttLoad.LoadDimensions.Width),
            p => p.Dimension!.Length.ShouldBe(mqttLoad.LoadDimensions.Length),
            p => p.Dimension!.Height.ShouldBe(mqttLoad.LoadDimensions.Height)
        );
    }

    [TestMethod]
    public void Map_ShouldMapGuidToCargoCorrectly()
    {
        var guidValue = Guid.NewGuid();

        var cargo = _mapper.Map<Cargo>(guidValue);

        cargo.ShouldSatisfyAllConditions(
            p => p.ShouldNotBeNull(),
            p => p.AgvCorrelationId.ShouldBe(guidValue)
        );
    }

    [TestMethod]
    public void Map_ShouldMapMqttLoadDimensionsToCuboidCorrectly()
    {
        var loadDimensions = new MqttLoadDimensions { Width = 2, Length = 3, Height = 4 };

        var cuboid = _mapper.Map<Cuboid>(loadDimensions);

        cuboid.ShouldSatisfyAllConditions(
            p => p.ShouldNotBeNull(),
            p => p.Width.ShouldBe(loadDimensions.Width),
            p => p.Length.ShouldBe(loadDimensions.Length),
            p => p.Height.ShouldBe(loadDimensions.Height)
        );
    }

    [TestMethod]
    public void Map_ShouldMapMqttBoundingBoxReferenceToPoint3DCorrectly()
    {
        var boundingBoxReference = new MqttBoundingBoxReference { X = 1, Y = 2, Z = 3, Theta = 0.5f };

        var point3D = _mapper.Map<Point3D>(boundingBoxReference);

        point3D.ShouldSatisfyAllConditions(
            p => p.ShouldNotBeNull(),
            p => p.X.ShouldBe(boundingBoxReference.X),
            p => p.Y.ShouldBe(boundingBoxReference.Y),
            p => p.Z.ShouldBe(boundingBoxReference.Z),
            p => ((float?)p.Theta).ShouldBe(boundingBoxReference.Theta)
        );
    }
}