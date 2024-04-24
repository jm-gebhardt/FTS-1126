using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Messages.MqttMessages;
using Gebhardt.Acc.DeviceCommunication.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Profiles;

[TestClass]
public class ErrorProfileTest
{
    [TestMethod]
    public void Map_WhereMqttMessagesErrorIsMappedToCommonMessagesError_ShouldMapAllPropertiesCorrectly()
    {
        var mqttMessagesError = new MqttError
        {
            ErrorDescription = "error description",
            ErrorLevel = MqttErrorLevel.Fatal,
            ErrorReferences = new List<MqttErrorReference>
            {
                new() { ReferenceKey = "testKey", ReferenceValue = "testValue" }
            },
            ErrorType = "error type"
        };

        var mapper = MappingHelper.GetMapper(new MqttErrorProfile());
        var commonMessagesError = mapper.Map<AgvError>(mqttMessagesError);

        commonMessagesError.ShouldSatisfyAllConditions(
            e => e.ErrorDescription.ShouldBe(mqttMessagesError.ErrorDescription),
            e => nameof(e.ErrorLevel).ShouldBe(nameof(mqttMessagesError.ErrorLevel)),
            e => e.ErrorFlag.ShouldBe(ErrorFlag.Agv),
            e => e.ErrorType.ShouldBe(mqttMessagesError.ErrorType),
            e => e.ErrorReferences.ShouldNotBeNull(),
            e => e.ErrorReferences!.First().ShouldSatisfyAllConditions(
                r => r.ReferenceKey.ShouldBe(mqttMessagesError.ErrorReferences!.First().ReferenceKey),
                r => r.ReferenceValue.ShouldBe(mqttMessagesError.ErrorReferences!.First().ReferenceValue)));
    }

    [TestMethod]
    public void Map_DescriptionContainsBattery_ShouldMapAllPropertiesCorrectly()
    {
        var mqttMessagesError = new MqttError
        {
            ErrorDescription = "error description with word battery in it",
            ErrorLevel = MqttErrorLevel.Fatal,
            ErrorReferences = new List<MqttErrorReference>
            {
                new() { ReferenceKey = "testKey", ReferenceValue = "testValue" }
            },
            ErrorType = "error type"
        };

        var mapper = MappingHelper.GetMapper(new MqttErrorProfile());
        var commonMessagesError = mapper.Map<AgvError>(mqttMessagesError);

        commonMessagesError.ShouldSatisfyAllConditions(
            e => e.ErrorDescription.ShouldBe(mqttMessagesError.ErrorDescription),
            e => nameof(e.ErrorLevel).ShouldBe(nameof(mqttMessagesError.ErrorLevel)),
            e => e.ErrorFlag.ShouldBe(ErrorFlag.Battery),
            e => e.ErrorType.ShouldBe(mqttMessagesError.ErrorType),
            e => e.ErrorReferences.ShouldNotBeNull(),
            e => e.ErrorReferences!.First().ShouldSatisfyAllConditions(
                r => r.ReferenceKey.ShouldBe(mqttMessagesError.ErrorReferences!.First().ReferenceKey),
                r => r.ReferenceValue.ShouldBe(mqttMessagesError.ErrorReferences!.First().ReferenceValue)));
    }
}