using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;

namespace Gebhardt.Acc.TransportOrder.Domain.Dictionaries;

/// <summary>
/// Provides a cache for the bounding boxes of all graph resources per AGV type.
/// </summary>
public interface IBoundingBoxCacheProvider : IConcurrentDictionaryProvider<string, IReadOnlyList<ResourceBoundingBox>>;