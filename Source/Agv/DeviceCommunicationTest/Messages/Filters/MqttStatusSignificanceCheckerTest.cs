using System.Collections;
using System.Reflection;
using Shouldly;
using AutoFixture;
using AutoFixture.Kernel;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Filters;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunicationTest.Messages.Filters;

[TestClass]
public sealed class MqttStatusSignificanceCheckerTests
{
    private static readonly Fixture _fixture = new();
    private readonly MqttStatusSignificanceChecker _checker =  new();

    private static MqttState CreateBaselineState() => _fixture.Create<MqttState>();

    private static T Clone<T>(T value)
    {
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(value);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json)!;
    }

    private static IEnumerable<object[]> GetPublicProperties() => typeof(MqttState)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => new object[] { p });

    private static void SetDifferentValue(PropertyInfo property, MqttState target)
    {
        var type = property.PropertyType;
        object? original = property.GetValue(target);

        // strings
        if (type == typeof(string))
        {
            property.SetValue(target, (string?) original + "_DIFF");
            return;
        }

        // booleans
        if (type == typeof(bool))
        {
            property.SetValue(target, !(bool) original!);
            return;
        }

        // numeric primitives
        if (type == typeof(int))
        {
            property.SetValue(target, (int) original! + 1);
            return;
        }
        if (type == typeof(float))
        {
            property.SetValue(target, (float) original! + 0.12345f);
            return;
        }

        // nullable primitives
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            if (underlying == typeof(int))
            {
                property.SetValue(target, ((int?) original ?? 0) + 1);
                return;
            }
            if (underlying == typeof(float))
            {
                property.SetValue(target, ((float?) original ?? 0f) + 0.12345f);
                return;
            }
            if (underlying == typeof(bool))
            {
                property.SetValue(target, !((bool?) original ?? false));
                return;
            }
        }

        // enums
        if (type.IsEnum)
        {
            object? different = Enum.GetValues(type).Cast<object>()
                .First(v => !v.Equals(original));
            property.SetValue(target, different);
            return;
        }

        // lists / arrays
        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            var list = (IList?) original ?? (IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(
                type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object)))!;

            list.Add(null); // a single extra element is enough for inequality
            property.SetValue(target, list);
            return;
        }

        // complex types
        object? current = Activator.CreateInstance(type);
        for (int attempt = 0; attempt < 10; attempt++)
        {
            object? candidate = _fixture.Create(type, new SpecimenContext(_fixture));

            if (!(current?.Equals(candidate) ?? candidate == null))
            {
                property.SetValue(target, candidate);
                return;
            }
        }

        throw new InvalidOperationException($"Cannot set a different value for property '{property.Name}' of type '{type.FullName}'.");
    }

    [TestMethod]
    public void HasSignificantChanges_ReturnsFalse_WhenObjectsAreEqual()
    {
        var baseline = CreateBaselineState();
        var copy = Clone(baseline);

        _checker.HasSignificantChanges(baseline, copy).ShouldBeFalse();
    }

    /// <summary>
    /// Ensure that a newly added property in MqttState automatically makes the test fail
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetPublicProperties))]
    public void HasSignificantChanges_ReturnsTrue_WhenSinglePropertyChanges(PropertyInfo property)
    {
        var original = CreateBaselineState();
        var modified = Clone(original);
        SetDifferentValue(property, modified);

        bool isSignificant = _checker.HasSignificantChanges(original, modified);

        isSignificant.ShouldBeTrue($"Property '{property.Name}' was modified but the checker returned false.");
    }
}