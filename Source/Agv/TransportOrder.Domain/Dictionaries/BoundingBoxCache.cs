using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;

namespace Gebhardt.Acc.TransportOrder.Domain.Dictionaries;

/// <inheritdoc cref="IBoundingBoxCacheProvider"/>
public class BoundingBoxCache : ConcurrentDictionaryProvider<string, IReadOnlyList<ResourceBoundingBox>>, IBoundingBoxCacheProvider;