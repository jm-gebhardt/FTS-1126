using Gebhardt.Acc.Common.Filter.ReadModelParser;
using static Gebhardt.Acc.Common.Filter.ReadModelParser.FilterReadModelBinderBase;
using JetBrains.Annotations;

namespace Gebhardt.Acc.CommonTest.Filter;

[UsedImplicitly]
internal class FilterReadModelTestHelperReadModel : FilterableTestHelper, IFilterReadModel<FilterableTestHelper, FilterReadModelTestHelperReadModel>
{
    [FilterProperty]
    [UsedImplicitly]
    public new string StringValue { get; init; } = "Test";

    [FilterProperty]
    [UsedImplicitly]
    public new IEnumerable<string> StringCollectionValue { get; init; } = ["Test"];

    [NavigationProperty]
    [UsedImplicitly]
    public new SubPropertyHelperReadModel SubPropertyValue { get; init; } = new();

    [NavigationProperty]
    [UsedImplicitly]
    public new IEnumerable<SubPropertyHelperReadModel> SubPropertyCollectionValue { get; init; } = [];

    [UsedImplicitly]
    public new int IgnoredValue { get; init; } = 10;
}

[UsedImplicitly]
internal class SubPropertyHelperReadModel : SubPropertyHelper, IFilterReadModel<SubPropertyHelper, SubPropertyHelperReadModel>
{
    [FilterProperty]
    [UsedImplicitly]
    public new string StringValue { get; init; } = "Test";

    [UsedImplicitly]
    public new int IgnoredValue { get; } = 10;

    [FilterProperty]
    [UsedImplicitly]
    public new DateTime Date { get; } = DateTime.UtcNow;
}

[UsedImplicitly]
internal class FilterableTestHelper
{
    [UsedImplicitly]
    public string StringValue { get; init; } = "Test";

    [UsedImplicitly]
    public List<string> StringCollectionValue { get; init; } = ["Test"];

    [UsedImplicitly]
    public SubPropertyHelper SubPropertyValue { get; init; } = new();

    [UsedImplicitly]
    public List<SubPropertyHelper> SubPropertyCollectionValue { get; init; } = [new()];

    [UsedImplicitly]
    public int IgnoredValue { get; init; } = 10;
}

[UsedImplicitly]
internal class SubPropertyHelper
{
    [UsedImplicitly]
    public string StringValue { get; init; } = "Test";

    [UsedImplicitly]
    public int IgnoredValue { get; } = 10;

    [UsedImplicitly]
    public DateTime Date { get; } = DateTime.UtcNow;
}