using AutoMapper;
using AutoMapper.Internal;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Configurations;

[TestClass]
public class AutoMapperMaxDepthConfigurationTest
{
    private IMapper _mapper = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SelfReferencing, SelfReferencing>();
            cfg.Internal().ForAllMaps((_, map) => map.MaxDepth(10));
        });

        _mapper = config.CreateMapper();
    }

    [TestMethod]
    public void Map_SelfReferencingObjectDeeperThanMaxDepth_TruncatesAtMaxDepth()
    {
        // Arrange - build a chain 20 levels deep
        var root = new SelfReferencing();
        var current = root;
        for (int i = 0; i < 20; i++)
        {
            current.Child = new SelfReferencing();
            current = current.Child;
        }

        // Act
        var result = _mapper.Map<SelfReferencing>(root);

        // Assert - walk the result, should stop at depth 10
        var node = result;
        int depth = 0;
        while (node?.Child != null)
        {
            node = node.Child;
            depth++;
        }

        depth.ShouldBeLessThanOrEqualTo(10);
        depth.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public void Map_SelfReferencingObjectWithinMaxDepth_MapsCompletely()
    {
        // Arrange - build a chain 5 levels deep (within MaxDepth 10)
        var root = new SelfReferencing();
        var current = root;
        for (int i = 0; i < 5; i++)
        {
            current.Child = new SelfReferencing();
            current = current.Child;
        }

        // Act
        var result = _mapper.Map<SelfReferencing>(root);

        // Assert - all 5 levels should be mapped
        var node = result;
        int depth = 0;
        while (node?.Child != null)
        {
            node = node.Child;
            depth++;
        }

        depth.ShouldBe(5);
    }

    private class SelfReferencing
    {
        public SelfReferencing? Child { get; set; }
    }
}
