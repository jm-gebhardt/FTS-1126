using Gebhardt.Acc.Common.Services;
using JetBrains.Annotations;

namespace Gebhardt.Acc.CommonTest.Services;

internal class TestMessageSignificanceChecker : ReflectionSignificanceChecker<TestMessage?>
{
    protected override Dictionary<string, Func<TestMessage?, TestMessage?, bool>> GetSignificantChangeByPropertyName()
    {
        return new Dictionary<string, Func<TestMessage?, TestMessage?, bool>>
        {
            { nameof(TestMessage.CustomProperty), (oldProduct, newProduct) => Math.Abs(oldProduct!.CustomProperty - newProduct!.CustomProperty) > 10 }
        };
    }
}

internal class TestMessage
{
    [UsedImplicitly]
    public double CustomProperty { get; set; }

    [UsedImplicitly]
    public bool DefaultProperty { get; set; }
}