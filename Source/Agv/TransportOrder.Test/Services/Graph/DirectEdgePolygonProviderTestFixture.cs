using Gebhardt.Acc.TransportOrder.Domain.Services.Graph;

namespace Gebhardt.Acc.TransportOrder.Test.Services.Graph;

public class DirectEdgePolygonProviderTestFixture
{
    public const double AgvLength = 2.0;
    public const double AgvWidth = 1.0;
    public const int PointDegree = 0;
    public const int StraightDegree = 1;
    public const int QuadraticDegree = 2;
    public const int CubicDegree = 3;

    public DirectEdgePolygonProvider Provider { get; } = new();
}