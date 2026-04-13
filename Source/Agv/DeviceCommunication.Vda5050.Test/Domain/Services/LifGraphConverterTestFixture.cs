using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

internal class LifGraphConverterTestFixture : FixtureBase
{
    public Mock<INurbsEdgeBuilder> NurbsEdgeBuilder { get; } = new();

    public LifGraphConverter Converter { get; }

    public LifGraphConverterTestFixture()
    {
        Converter = new LifGraphConverter(
            NurbsEdgeBuilder.Object,
            Mock.Of<ILogger<LifGraphConverter>>());
    }
}