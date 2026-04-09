using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class BackgroundGenerator
{
    public static readonly Guid DefaultId = new("a75e7437-f0a0-fb05-1452-2c5cc7c1ba3f");
    public static readonly Guid DefaultMapId = new("d75e7437-f0a0-fb05-1452-2c5cc7c1ba3d");

    public static Background Generate(
        Guid id = default,
        string name = "TestBackground",
        Guid? mapId = null)
    {
        if (id == Guid.Empty)
        {
            id = DefaultId;
        }

        var background = new Background(id)
        {
            Name = name,
            MapId = mapId ?? DefaultMapId
        };

        AddObjects(background);
        AddLayers(background);
        AddColorEntries(background);

        return background;
    }

    private static void AddObjects(Background background)
    {
        var rectangle = new RectangleObject(
            backgroundId: background.Id,
            externalId: 1,
            x: -178.96546419588196,
            y: -478.0546289266873,
            width: 102.10745054866265,
            height: 132.01771384069514,
            rotation: 0.9135803672511803,
            scaleX: 1.1559371968720624,
            scaleY: 0.9999999999999982,
            color: 14024704,
            borderWidth: 0,
            borderColor: 6858239);
        background.Objects.Add(rectangle);

        var triangle = new TriangleObject(
            backgroundId: background.Id,
            externalId: 2,
            x: -261.4025364938212,
            y: -281.7942856433123,
            width: 128.92354867255386,
            height: 142.3315977344995,
            rotation: 0,
            scaleX: 1,
            scaleY: 1,
            color: 7828931,
            borderWidth: 5,
            borderColor: 15174144);
        background.Objects.Add(triangle);

        var circle = new CircleObject(
            backgroundId: background.Id,
            externalId: 3,
            x: -145.8870368832129,
            y: -326.1439863866708,
            width: 192.86962881414058,
            height: 192.86962881414058,
            rotation: 0,
            scaleX: 2.4919786096256686,
            scaleY: 1,
            color: 2502198,
            borderWidth: 0,
            borderColor: null);
        background.Objects.Add(circle);

        var line1 = new LineObject(
            backgroundId: background.Id,
            externalId: 4,
            x: -198.22968085993682,
            y: -125.02325045748677,
            width: 129.9549370619343,
            height: 226.90544566369482,
            rotation: 0,
            scaleX: 1.322390013383921,
            scaleY: 1,
            color: 1744416,
            lineWidth: 8,
            lineStyle: LineStyle.Solid);
        line1.Points.Add(new BackgroundObjectPoint(line1.Id, 0, 0, 226.90544566369482));
        line1.Points.Add(new BackgroundObjectPoint(line1.Id, 1, 129.9549370619343, 0));
        background.Objects.Add(line1);

        var line2 = new LineObject(
            backgroundId: background.Id,
            externalId: 5,
            x: -145.45132125991609,
            y: 350.6300069599582,
            width: 116.54688799998871,
            height: 221.74850371679264,
            rotation: -2.4630863216721175,
            scaleX: 1.3083540591775054,
            scaleY: 0.9999999999999971,
            color: 2502198,
            lineWidth: 1,
            lineStyle: LineStyle.Dashed);
        line2.Points.Add(new BackgroundObjectPoint(line2.Id, 0, 0, 221.74850371679264));
        line2.Points.Add(new BackgroundObjectPoint(line2.Id, 1, 116.54688799998871, 0));
        background.Objects.Add(line2);

        var polyline1 = new PolylineObject(
            backgroundId: background.Id,
            externalId: 6,
            x: -53.062081838973995,
            y: -4.3508088999763,
            width: 178.43019136281453,
            height: 292.9143025840424,
            rotation: 0,
            scaleX: 1,
            scaleY: 1,
            color: 31084,
            lineWidth: 2,
            lineStyle: LineStyle.Solid);
        polyline1.Points.Add(new BackgroundObjectPoint(polyline1.Id, 0, 0, 248.56460184068385));
        polyline1.Points.Add(new BackgroundObjectPoint(polyline1.Id, 1, 108.2957808849452, 0));
        polyline1.Points.Add(new BackgroundObjectPoint(polyline1.Id, 2, 105.20161571680387, 235.15655277873827));
        polyline1.Points.Add(new BackgroundObjectPoint(polyline1.Id, 3, 151.61409323892326, 4.125553557521698));
        polyline1.Points.Add(new BackgroundObjectPoint(polyline1.Id, 4, 178.43019136281453, 254.7529321769664));
        polyline1.Points.Add(new BackgroundObjectPoint(polyline1.Id, 5, 3.094165168141217, 245.47043667254252));
        polyline1.Points.Add(new BackgroundObjectPoint(polyline1.Id, 6, 3.094165168141217, 245.47043667254252));
        polyline1.Points.Add(new BackgroundObjectPoint(polyline1.Id, 7, 112.4213344424669, 292.9143025840424));
        background.Objects.Add(polyline1);

        var polyline2 = new PolylineObject(
            backgroundId: background.Id,
            externalId: 7,
            x: 349.50925563802673,
            y: 233.96673924033564,
            width: 88.69940148671708,
            height: 277.44347674333596,
            rotation: -2.926629690109585,
            scaleX: 1.7139526811673242,
            scaleY: 0.9999999999999989,
            color: 4623345,
            lineWidth: 20,
            lineStyle: LineStyle.Dashed);
        polyline2.Points.Add(new BackgroundObjectPoint(polyline2.Id, 0, 0, 216.59156176989052));
        polyline2.Points.Add(new BackgroundObjectPoint(polyline2.Id, 1, 26.81609812389115, 0));
        polyline2.Points.Add(new BackgroundObjectPoint(polyline2.Id, 2, 45.38108913273891, 234.1251643893578));
        polyline2.Points.Add(new BackgroundObjectPoint(polyline2.Id, 3, 84.57384792919527, 5.1569419469021796));
        polyline2.Points.Add(new BackgroundObjectPoint(polyline2.Id, 4, 88.69940148671708, 277.44347674333596));
        background.Objects.Add(polyline2);

        var text1 = new TextObject(
            backgroundId: background.Id,
            externalId: 8,
            x: 313.08079639107893,
            y: -144.61962985571495,
            width: 0,
            height: 0,
            rotation: 0,
            scaleX: 1,
            scaleY: 1,
            color: 2502198,
            text: "text examples default",
            fontSize: 16,
            fontFamily: "roboto");
        background.Objects.Add(text1);

        var text2 = new TextObject(
            backgroundId: background.Id,
            externalId: 9,
            x: 561.6532761683371,
            y: -141.48780684347622,
            width: 0,
            height: 0,
            rotation: 2.022779706154159,
            scaleX: 0.9999999999999982,
            scaleY: 0.9999999999999982,
            color: 14024704,
            text: "text example custom",
            fontSize: 50,
            fontFamily: "monospace");
        background.Objects.Add(text2);
    }

    private static void AddLayers(Background background)
    {
        background.Layers.Add(new BackgroundLayer(background.Id, 1, "rectangle #1", visible: true, locked: false));
        background.Layers.Add(new BackgroundLayer(background.Id, 2, "triangle #2", visible: true, locked: false));
        background.Layers.Add(new BackgroundLayer(background.Id, 3, "circle #3", visible: false, locked: false));
        background.Layers.Add(new BackgroundLayer(background.Id, 4, "line #4", visible: true, locked: false));
        background.Layers.Add(new BackgroundLayer(background.Id, 5, "line #5", visible: true, locked: true));
        background.Layers.Add(new BackgroundLayer(background.Id, 6, "polyline #6", visible: true, locked: false));
        background.Layers.Add(new BackgroundLayer(background.Id, 7, "polyline #7", visible: false, locked: false));
        background.Layers.Add(new BackgroundLayer(background.Id, 8, "text #8", visible: true, locked: true));
        background.Layers.Add(new BackgroundLayer(background.Id, 9, "text #9", visible: true, locked: true));
    }

    private static void AddColorEntries(Background background)
    {
        background.ColorEntries.Add(new BackgroundColorEntry(background.Id, ColorPaletteType.Line, 12345));
        background.ColorEntries.Add(new BackgroundColorEntry(background.Id, ColorPaletteType.Line, 12356));
        background.ColorEntries.Add(new BackgroundColorEntry(background.Id, ColorPaletteType.Fill, 2345));
    }
}
