using Gebhardt.Acc.TransportOrder.Domain.Models.Views;
using Shouldly;
using Fixture = Gebhardt.Acc.TransportOrder.Test.Domain.Models.Views.ViewTestFixture;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models.Views;

[TestClass]
public class ViewTest
{
    [TestMethod]
    public void CreateGenerated_NoLevel_ReturnsSingleEmptyDefaultView()
    {
        var views = View.CreateGenerated([]);

        var view = views.ShouldHaveSingleItem();
        view.Name.ShouldBe(Fixture.DefaultViewName);
        view.LevelId.ShouldBeNull();
        view.IsDefault.ShouldBeTrue();
        view.IsGenerated.ShouldBeTrue();
        view.Background.ShouldBeNull();
    }

    [TestMethod]
    public void CreateGenerated_TwoLevels_ReturnsOneViewPerLevelOrderedByLevelWithFirstAsDefault()
    {
        var views = View.CreateGenerated([new LevelArea(Fixture.SecondLevelId, Fixture.SecondLevelArea), new LevelArea(Fixture.FirstLevelId, Fixture.FirstLevelArea)]);

        views.Select(v => v.LevelId).ShouldBe([Fixture.FirstLevelId, Fixture.SecondLevelId]);
        views.Select(v => v.Name).ShouldBe([Fixture.FirstLevelId, Fixture.SecondLevelId]);
        views.Select(v => v.IsDefault).ShouldBe([true, false]);
        views.ShouldAllBe(v => v.IsGenerated);
    }

    [TestMethod]
    public void CreateGenerated_Level_EnlargesTheLevelAreaByTheMargin()
    {
        var view = Fixture.CreateGeneratedView();

        view.Area.ShouldBe(Fixture.FirstLevelArea.Enlarge(Fixture.GeneratedAreaMargin));
    }

    [TestMethod]
    public void CreateGenerated_SameLevel_ReturnsSameId()
    {
        var view = Fixture.CreateGeneratedView();

        view.Id.ShouldBe(Fixture.CreateGeneratedView().Id);
    }

    [TestMethod]
    public void Update_GeneratedView_AppliesChangesAndIsNoLongerGenerated()
    {
        var view = Fixture.CreateGeneratedView();

        view.Update(Fixture.NewName, null, null, null);

        view.Name.ShouldBe(Fixture.NewName);
        view.LevelId.ShouldBe(Fixture.FirstLevelId);
        view.IsGenerated.ShouldBeFalse();
    }

    [TestMethod]
    public void SetBackground_GeneratedView_SetsBackgroundAndIsNoLongerGenerated()
    {
        var view = Fixture.CreateGeneratedView();

        view.SetBackground(Fixture.Background);

        view.Background.ShouldBe(Fixture.Background);
        view.IsGenerated.ShouldBeFalse();
    }

    [TestMethod]
    public void Union_TwoAreas_ReturnsTheSmallestAreaContainingBoth()
    {
        var union = Fixture.FirstLevelArea.Union(Fixture.SecondLevelArea);

        union.ShouldBe(new ViewArea(Fixture.SecondLevelArea.MinX, Fixture.FirstLevelArea.MinY, Fixture.FirstLevelArea.MaxX, Fixture.FirstLevelArea.MaxY));
    }
}