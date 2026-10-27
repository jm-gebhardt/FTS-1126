using Gebhardt.Acc.TransportOrder.Domain.Models.Views;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models.Views;

public static class ViewTestFixture
{
    public const string FirstLevelId = "A";
    public const string SecondLevelId = "B";
    public const string DefaultViewName = "Default";
    public const string NewName = "Hall";
    public const double GeneratedAreaMargin = 1.0;

    public static ViewArea FirstLevelArea { get; } = new(0, 0, 10, 5);

    public static ViewArea SecondLevelArea { get; } = new(-2, 1, 3, 4);

    public static ViewBackground Background { get; } = new(0.05, 1, 2);

    public static View CreateGeneratedView()
    {
        return View.CreateGenerated([new LevelArea(FirstLevelId, FirstLevelArea)]).Single();
    }
}