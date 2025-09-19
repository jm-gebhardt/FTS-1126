using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Exceptions;
using Gebhardt.Acc.Common.Filter.Expressions;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Common.Filter.ReadModelParser;

/// <summary>
/// Builds a filter for the combination of <typeparamref name="TDomainObject"/> and <typeparamref name="TReadModel"/>.
/// </summary>
/// <typeparam name="TDomainObject">The domain object to be filtered.</typeparam>
/// <typeparam name="TReadModel">The read model for <typeparamref name="TDomainObject"/> that will be returned from the filter queries and describes what can be filtered.</typeparam>
[PublicAPI]
public sealed class Filter<TDomainObject, TReadModel> : Filter where TReadModel : class, TDomainObject, IFilterReadModel<TDomainObject, TReadModel>, new() where TDomainObject : class
{
    private Filter()
    {
    }

    /// <summary>
    /// A list of all the keys of this entity.
    /// </summary>
    // ReSharper disable once StaticMemberInGenericType // Intentional, as every Filterable<TEntity> needs its own keys.
    private static List<string> _keyProperties = GetShadowingProperties()
        .Where(properties => Attribute.IsDefined(properties.Item2, typeof(KeyProperty)))
        .Select(p => p.Item2.Name)
        .ToList();

    // ReSharper disable once StaticMemberInGenericType // Intentional, as every Filterable<TEntity> needs its own key accessors.
    private static readonly Dictionary<string, LambdaExpression> _keyAccessors = new();

    /// <summary>
    /// A dictionary containing the property accessors for the read model of the entity.
    /// </summary>
    // ReSharper disable once StaticMemberInGenericType // Intentional here, as every Filterable<TEntity> needs its own accessors.
    internal static readonly Dictionary<string, LambdaExpression> PropertyAccessors = new();

    /// <summary>
    /// A dictionary containing the property accessors for the entity where the type in the overload of the read model matches the type in the domain model.
    /// </summary>
    // ReSharper disable once StaticMemberInGenericType // Intentional here, as every Filterable<TEntity> needs its own accessors.
    private static readonly Dictionary<string, LambdaExpression> _domainPropertyAccessors = new();

    /// <summary>
    /// Dictionary of all the parsed filterables that contain join information.
    /// </summary>
    // ReSharper disable once StaticMemberInGenericType // Intentional here, as every Filterable<TEntity> needs its own join properties.
    private static readonly Dictionary<string, IParsedFilterable> _joinProperties = new();

    private static readonly ParsedFilterable<TDomainObject, TReadModel> _parsedFilterable = new();

    /// <summary>
    /// Verifies the parsed filterable against the provided values.
    /// </summary>
    public static bool VerifyParsing(int leafCount, int collectionLeafCount, int subTreeCount, int collectionSubTreeCount)
    {
        return _parsedFilterable.Leaves.Count == leafCount &&
               _parsedFilterable.CollectionLeaves.Count == collectionLeafCount &&
               _parsedFilterable.SubTrees.Count == subTreeCount &&
               _parsedFilterable.CollectionSubTrees.Count == collectionSubTreeCount;
    }

    static Filter()
    {
        CreateAccessorMap();

        _includeLayers.Sort(static (s1, s2) => s1.Count(static c => c == '.').CompareTo(s2.Count(static c => c == '.')));
        _joinLayers.Sort(static (s1, s2) => s1.Count(static c => c == '.').CompareTo(s2.Count(static c => c == '.')));

        _joinLayers.Insert(0, FilterBase.ReadModelFilterLayer);

        _totalLayers = _includeLayers.Concat(_joinLayers).ToList();
    }

    private static void CreateAccessorMap()
    {
        // Empty string always self-references the base entity by convention.
        PropertyAccessors.Add(FilterBase.DomainFilterLayer, FilterExpressionBuilder.CreateIdentityExpression<TReadModel>());
        _domainPropertyAccessors.Add(FilterBase.DomainFilterLayer, FilterExpressionBuilder.CreateIdentityExpression<TDomainObject>());

        const string accumulatedName = FilterBase.DomainFilterLayer;

        CreateAccessorMap(_parsedFilterable, accumulatedName, true);

        CreateProjections(_parsedFilterable);

        CreateKeyAccessors();
    }

    private static void CreateKeyAccessors()
    {
        foreach (string key in _keyProperties)
        {
            if (PropertyAccessors.TryGetValue(key, out var accessor))
            {
                _keyAccessors.Add(key, accessor);
            }
            else
            {
                var type = typeof(TReadModel).GetProperty(key)?.PropertyType;

                if (type == null)
                {
                    throw new FilterParserException($"Could not create type for {key}");
                }

                accessor = FilterExpressionBuilder.CreatePropertyAccess(typeof(TReadModel), type, key);

                if (accessor == null)
                {
                    throw new FilterParserException($"Could not create accessor for {key}");
                }

                _keyAccessors.Add(key, accessor);
            }
        }
    }

    private static void CreateAccessorMap(IParsedFilterable parsedFilterable, string accumulatedName, bool canStillAccessDomain)
    {
        bool isRoot = accumulatedName == string.Empty;
        string extendedAccumulatedName = accumulatedName + (isRoot ? "" : FilterBase.AccessLevelSeparator);

        foreach (var leaf in parsedFilterable.Leaves.Concat(parsedFilterable.CollectionLeaves))
        {
            var accessor = leaf.AccessorExpression;

            if (leaf is not IParsedFilterableCollectionLeaf)
            {
                accessor = TryMergeAccessors(accumulatedName, accessor, PropertyAccessors);
            }

            PropertyAccessors.Add(extendedAccumulatedName + leaf.Name, accessor);

            if (leaf.DomainAccessorExpression == null || !canStillAccessDomain)
            {
                continue;
            }

            var domAccessor = leaf.DomainAccessorExpression;

            if (leaf is not IParsedFilterableCollectionLeaf)
            {
                domAccessor = TryMergeAccessors(accumulatedName, domAccessor, _domainPropertyAccessors);
            }

            _domainPropertyAccessors.Add(extendedAccumulatedName + leaf.Name, domAccessor);
        }

        foreach (var parsedFilterableSubTree in parsedFilterable.SubTrees.Concat(parsedFilterable.CollectionSubTrees))
        {
            string newName = extendedAccumulatedName + parsedFilterableSubTree.Name;

            var accessor = parsedFilterableSubTree.AccessorExpression;

            if (parsedFilterableSubTree is not IParsedFilterableCollectionSubTree)
            {
                accessor = TryMergeAccessors(accumulatedName, accessor, PropertyAccessors);
            }

            if (parsedFilterableSubTree.SubTree.IsJoin)
            {
                _joinLayers.Add(newName);
                _joinProperties.Add(newName, parsedFilterableSubTree.SubTree);
            } 
            else
            {
                _includeLayers.Add(newName);
            }

            PropertyAccessors.Add(newName, accessor);

            if (parsedFilterableSubTree.DomainAccessorExpression != null && canStillAccessDomain)
            {
                var domAccessor = parsedFilterableSubTree.DomainAccessorExpression;

                if (parsedFilterableSubTree is not IParsedFilterableCollectionSubTree)
                {
                    domAccessor = TryMergeAccessors(accumulatedName, domAccessor, _domainPropertyAccessors);
                }

                _domainPropertyAccessors.Add(newName, domAccessor);
            }

            CreateAccessorMap(parsedFilterableSubTree.SubTree, newName, canStillAccessDomain && parsedFilterableSubTree.DomainAccessorExpression != null);
        }
    }

    private static LambdaExpression TryMergeAccessors(string accumulatedName, LambdaExpression accessor, Dictionary<string, LambdaExpression> accessors)
    {
        if (!accessors.TryGetValue(accumulatedName, out var accumulatedAccessor) || !accumulatedAccessor.ReturnType.IsAssignableTo(accessor.Parameters.First().Type))
        {
            return accessor;
        }

        return FilterExpressionBuilder.Compose(accumulatedAccessor, accessor);
    }

    private static void CreateProjections(IParsedFilterable filterable)
    {
        foreach (var subTree in filterable.SubTrees.Concat(filterable.CollectionSubTrees))
        {
            CreateProjections(subTree.SubTree);
            CreateProjection(subTree.SubTree);
        }

        CreateProjection(filterable);
    }

    private static void CreateProjection(IParsedFilterable filterable)
    {
        Type[] treeTypes = filterable.GetType().GetGenericArguments();

        if (treeTypes is not [.., { } domModel, { } readModel] || domModel == readModel || TypeMappings.ContainsKey(new Tuple<Type, Type>(domModel, readModel)))
        {
            return;
        }

        var newProjection = FilterExpressionBuilder.CreateMemberInitializerExpression(domModel, readModel, TypeMappings, filterable.MemberMappings);

        TryAddTypeMapping(domModel, readModel, newProjection);
    }

    // ReSharper disable once StaticMemberInGenericType // Intentional, as all Filterable are supposed to have their own layers.
    private static readonly List<string> _includeLayers = [];

    // ReSharper disable once StaticMemberInGenericType // Intentional, as all Filterable are supposed to have their own layers.
    private static readonly List<string> _joinLayers = [];

    // ReSharper disable once StaticMemberInGenericType // Intentional, as all Filterable are supposed to have their own layers.
    private static readonly List<string> _totalLayers;

    /// <summary>
    /// Filters an entity by the provided filter.
    /// </summary>
    /// <param name="filter">The filter to use.</param>
    /// <returns>The filtered result.</returns>
    public delegate Task<ICollection<TReadModel>> FilterBy(FilterBase filter);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="filterBase"></param>
    /// <param name="pageNr"></param>
    /// <param name="pageSize"></param>
    /// <param name="ordering"></param>
    /// <param name="orderByProperties"></param>
    /// <returns></returns>
    public delegate Task<ICollection<TReadModel>> FilterByPaginatedWithOrdering(FilterBase filterBase, int? pageNr, int? pageSize, SortOrdering? ordering, ICollection<string> orderByProperties);

    private delegate IQueryable<TReadModel> ApplyFiltering<in TDbContext>(TDbContext context, FilterBase filter);

    private static ApplyFiltering<TDbContext> CreateFilterApplier<TDbContext>() where TDbContext : DbContext
    {
        return (context, filterBase) =>
        {
            Dictionary<int, FilterBase> batches = filterBase.SplitFilters(_totalLayers, _domainPropertyAccessors);

            IQueryable<TDomainObject> query = context.Set<TDomainObject>()
                .AsNoTracking();

            if (batches.TryGetValue(0, out var baseLevelFilter))
            {
                query = query.Where(baseLevelFilter.ToExpressionTree(_domainPropertyAccessors).AsPredicate<TDomainObject>());
            }

            for (int layerIndex = 1; layerIndex < _includeLayers.Count; layerIndex++)
            {
                query = query.Include(_includeLayers[layerIndex]);

                if (!batches.TryGetValue(layerIndex, out var filter))
                {
                    continue;
                }

                query = query.Where(filter.ToExpressionTree(_domainPropertyAccessors).AsPredicate<TDomainObject>());
            }

            if (!TryGetTypeMapping(typeof(TDomainObject), typeof(TReadModel), out var projection))
            {
                throw new FilterExecutionException($"Could not find projection for type {typeof(TDomainObject).FullName}");
            }

            IQueryable<TReadModel> projectedQuery = query.ApplyProjection<TReadModel, TDomainObject>(projection);

            if (batches.TryGetValue(_includeLayers.Count, out var readModelBase))
            {
                projectedQuery = projectedQuery.Where(readModelBase.ToExpressionTree(PropertyAccessors).AsPredicate<TReadModel>());
            }

            for (int layerIndex = _includeLayers.Count + 1; layerIndex < _totalLayers.Count; layerIndex++)
            {
                if (_joinProperties[_totalLayers[layerIndex]] is not
                    {
                        QueryableCreator: { } queryableCreator,
                        RootKeyAccessor: { } ownKeyAccessor,
                        OtherKeyAccessor: { } otherKeyAccessor,
                        MergeExpression: { } mergeExpression
                    })
                {
                    throw new FilterExecutionException($"Could not create join for {_totalLayers[layerIndex]}");
                }

                projectedQuery = projectedQuery.ApplyJoin(queryableCreator.Invoke(context), ownKeyAccessor, otherKeyAccessor, mergeExpression);

                if (batches.TryGetValue(layerIndex, out var predicate))
                {
                    projectedQuery = projectedQuery.Where(predicate.ToExpressionTree(PropertyAccessors).AsPredicate<TReadModel>());
                }
            }

            return projectedQuery;
        };
    }
    
    /// <summary>
    /// Creates a filter for the entity defined by the attributes <see cref="Filter.FilterProperty"/>, <see cref="Filter.JoinProperty"/>, <see cref="Filter.NavigationProperty"/> and <see cref="Filter.KeyProperty"/>
    /// </summary>
    /// <typeparam name="TDbContext">The db context type.</typeparam>
    /// <param name="contextFactory">The db context factory</param>
    /// <returns>The filter.</returns>
    [RequiresUnreferencedCode("Requires attributes on the TReadModel type definition that describe how the read model projects the entity (See non-generic Filter).")]
    public static FilterBy CreateFilter<TDbContext>(IDbContextFactory<TDbContext> contextFactory) where TDbContext : DbContext
    {
        ApplyFiltering<TDbContext> filterer = CreateFilterApplier<TDbContext>();

        return async delegate (FilterBase filterBase)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            return await filterer(context, filterBase).ToListAsync();
        };
    }

    /// <summary>
    /// Creates a filter for the entity defined by the attributes <see cref="Filter.FilterProperty"/>, <see cref="Filter.JoinProperty"/>, <see cref="Filter.NavigationProperty"/> and <see cref="Filter.KeyProperty"/>
    /// </summary>
    /// <typeparam name="TDbContext">The db context type.</typeparam>
    /// <param name="contextFactory">The db context factory</param>
    /// <returns>The filter.</returns>
    [RequiresUnreferencedCode("Requires attributes on the TReadModel type definition that describe how the read model projects the entity (See non-generic Filter).")]
    public static FilterByPaginatedWithOrdering CreateFilterWithPagination<TDbContext>(IDbContextFactory<TDbContext> contextFactory) where TDbContext : DbContext
    {
        ApplyFiltering<TDbContext> filterer = CreateFilterApplier<TDbContext>();

        return async delegate (FilterBase filterBase, int? pageNr, int? pageSize, SortOrdering? ordering, ICollection<string> orderByProperties) 
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            IQueryable<TReadModel> projectedQuery = filterer(context, filterBase);

            IOrderedQueryable<TReadModel>? orderedQuery = null;

            List<string> incomingKeys = orderByProperties.Where(s => _keyProperties.Contains(s)).ToList();

            IEnumerable<string> incomingProps = orderByProperties.Where(s => !_keyProperties.Contains(s));

            IEnumerable<string> remainingKeys = _keyProperties.Except(incomingKeys);

            foreach (string prop in incomingProps)
            {
                if (!PropertyAccessors.TryGetValue(prop, out var accessor))
                {
                    continue;
                }
                if (orderedQuery == null)
                {
                    orderedQuery = projectedQuery.OrderByAccessor(accessor, ordering);
                    continue;
                }
                orderedQuery = orderedQuery.ThenOrderByAccessor(accessor);
            }

            foreach (string key in incomingKeys)
            {
                if (!_keyAccessors.TryGetValue(key, out var accessor))
                {
                    continue;
                }
                if (orderedQuery == null)
                {
                    orderedQuery = projectedQuery.OrderByAccessor(accessor, ordering);
                    continue;
                }
                orderedQuery = orderedQuery.ThenOrderByAccessor(accessor);
            }

            foreach (string key in remainingKeys)
            {
                if (!_keyAccessors.TryGetValue(key, out var accessor))
                {
                    continue;
                }
                if (orderedQuery == null)
                {
                    orderedQuery = projectedQuery.OrderByAccessor(accessor, ordering);
                    continue;
                }
                orderedQuery = orderedQuery.ThenOrderByAccessor(accessor);
            }

            projectedQuery = orderedQuery as IQueryable<TReadModel> ?? projectedQuery;

            bool usePagination = pageNr != null && pageSize != null;
            if (usePagination)
            {
                projectedQuery = projectedQuery.Skip(pageNr!.Value * pageSize!.Value)
                    .Take(pageSize.Value);
            }

            return await projectedQuery.ToListAsync();
        };
    }
    
    private static List<Tuple<PropertyInfo, PropertyInfo>> GetShadowingProperties()
    {
        PropertyInfo[] shadowing = typeof(TReadModel).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        PropertyInfo[] originals = typeof(TDomainObject).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        List<Tuple<PropertyInfo, PropertyInfo>> result = [];

        foreach (var newProperty in shadowing)
        {
            var originalProperty = originals.FirstOrDefault(po => po.Name == newProperty.Name);

            if (originalProperty == null)
            {
                continue;
            }

            result.Add(new Tuple<PropertyInfo, PropertyInfo>(originalProperty, newProperty));
        }

        return result;
    }

    /// <summary>
    /// Parsed version of this Filterable.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TRm"></typeparam>
    private class ParsedFilterable<T, TRm> : IParsedFilterable where TRm : class, T, IFilterReadModel<T, TRm>, new() where  T : class
    {
        public List<IParsedFilterableLeaf> Leaves { get; } = [];

        public List<IParsedFilterableCollectionLeaf> CollectionLeaves { get; } = [];

        public List<IParsedFilterableSubTree> SubTrees { get; } = [];

        public List<IParsedFilterableCollectionSubTree> CollectionSubTrees { get; } = [];

        public Dictionary<string, string> MemberMappings { get; } = new();

        public bool IsJoin => OtherKeyAccessor != null && RootKeyAccessor != null && QueryableCreator != null && MergeExpression != null;

        public LambdaExpression? RootKeyAccessor { get; }

        public LambdaExpression? OtherKeyAccessor { get; }

        public Func<DbContext, IQueryable>? QueryableCreator { get; }

        public LambdaExpression? MergeExpression { get; }

        public ParsedFilterable(string ownKey, string otherKey, string name) : this(true)
        {
            var ownKeyType = typeof(TReadModel).GetProperty(ownKey)?.PropertyType;
            var otherKeyType = typeof(TRm).GetProperty(otherKey)?.PropertyType;

            if (otherKeyType == null || ownKeyType == null)
            {
                return;
            }

            bool ownKeyIsNullable = otherKeyType == Nullable.GetUnderlyingType(ownKeyType) && otherKeyType != ownKeyType;

            if (otherKeyType != ownKeyType && otherKeyType != Nullable.GetUnderlyingType(ownKeyType))
            {
                return;
            }

            RootKeyAccessor = FilterExpressionBuilder.CreatePropertyAccess(typeof(TReadModel), ownKeyType, ownKey);

            OtherKeyAccessor = ownKeyIsNullable ? 
                FilterExpressionBuilder.CreatePropertyAccessWithCast(typeof(T), ownKeyType, otherKey) : 
                FilterExpressionBuilder.CreatePropertyAccess(typeof(T), otherKeyType, otherKey);
            
            QueryableCreator = context => context.Set<T>().AsNoTracking();
            
            if (!TryGetTypeMapping(typeof(T), typeof(TRm), out var memberInitializer))
            {
                memberInitializer = FilterExpressionBuilder.CreateMemberInitializerExpression<T, TRm>(TypeMappings, MemberMappings);
            }
            MergeExpression = FilterExpressionBuilder.CreateInitializerWithMixin<TReadModel, T, TRm>(name, memberInitializer);
        }

        private ParsedFilterable(bool isJoin)
        {
            ExtractLeaves(isJoin);

            ExtractSubTrees(isJoin);
        }

        public ParsedFilterable() : this(false)
        {
        }

        private void ExtractSubTrees(bool isJoin)
        {
            List<Tuple<PropertyInfo, PropertyInfo>> subProperties = Filter<T,TRm>.GetShadowingProperties().Where(props => Attribute.IsDefined(props.Item2, typeof(InternalSubProperty))).ToList();

            foreach (var (originalProp, newProperty) in subProperties)
            {
                var newBasePropType = newProperty.PropertyType.GetEnumerableContentType() ?? newProperty.PropertyType;
                var originalBasePropType = originalProp.PropertyType.GetEnumerableContentType() ?? originalProp.PropertyType;
                
                bool canAccessDomainObject = newBasePropType == originalBasePropType && !isJoin;

                if (!typeof(IFilterReadModel).IsAssignableFrom(newBasePropType))
                {
                    continue;
                }

                var accessor = FilterExpressionBuilder.CreatePropertyAccess(typeof(TRm), newProperty.PropertyType, newProperty.Name);

                var domainAccessor = canAccessDomainObject ? FilterExpressionBuilder.CreatePropertyAccess(typeof(T), originalProp.PropertyType, newProperty.Name) : null;

                IParsedFilterable subTree;

                if (Attribute.GetCustomAttribute(newProperty, typeof(JoinProperty)) is JoinProperty joinProperty)
                {
                    if (newBasePropType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFilterReadModel<,>))?.GenericTypeArguments.FirstOrDefault() is not { } originalDomainModel)
                    {
                        throw new FilterParserException($"Could not parse property {newProperty.Name} for filterable {typeof(TRm).FullName}");
                    }

                    subTree = ParsedFilterableFromTypes(typeof(T), typeof(TRm), originalDomainModel, newBasePropType, joinProperty.OwnKeyProperty, joinProperty.OtherKeyProperty, newProperty.Name);

                    originalBasePropType = originalDomainModel;

                    MemberMappings.Add(joinProperty.OwnKeyProperty, newProperty.Name);
                }
                else
                {
                    subTree = ParsedFilterableFromTypes(typeof(T), typeof(TRm), originalBasePropType, newBasePropType);
                }

                if (newBasePropType != newProperty.PropertyType)
                {
                    CollectionSubTrees.Add(CollectionSubTreeFromTypes(typeof(T), typeof(TRm), newProperty.PropertyType, originalBasePropType, newBasePropType, newProperty.Name, accessor, subTree, domainAccessor));
                }
                else
                {
                    SubTrees.Add(SubTreeFromTypes(typeof(T), typeof(TRm), originalBasePropType, newBasePropType, newProperty.Name, accessor, subTree, domainAccessor));
                }
            }
        }

        private void ExtractLeaves(bool isJoin)
        {
            List<Tuple<PropertyInfo, PropertyInfo>> filterProperties = Filter<T, TRm>.GetShadowingProperties().Where(props => Attribute.IsDefined(props.Item2, typeof(FilterProperty)) && !Attribute.IsDefined(props.Item2, typeof(InternalSubProperty))).ToList();

            foreach (var (oldProperty, newProperty) in filterProperties)
            {
                var accessor = FilterExpressionBuilder.CreatePropertyAccess(typeof(TRm), newProperty.PropertyType, newProperty.Name);

                bool canAccessDomainObject = newProperty.PropertyType == oldProperty.PropertyType && !isJoin;

                var domainAccessor = canAccessDomainObject ? FilterExpressionBuilder.CreatePropertyAccess(typeof(T), newProperty.PropertyType, newProperty.Name) : null;

                var newCollectionType = newProperty.PropertyType.GetEnumerableContentType();

                if (newCollectionType != null)
                {
                    CollectionLeaves.Add(CollectionLeafFromTypes(typeof(T), typeof(TRm), newProperty.PropertyType, newCollectionType, newProperty.Name, accessor, domainAccessor));
                }
                else
                {
                    Leaves.Add(LeafFromTypes(typeof(T), typeof(TRm), newProperty.PropertyType, newProperty.Name, accessor, domainAccessor));
                }
            }
        }
    }

    private static IParsedFilterable ParsedFilterableFromTypes(Type root, Type rootReadModel, Type self, Type selfReadModel, string? rootKeyProperty = null, string? childKeyProperty = null, string? name = null)
    {
        object? parsed;

        var parsedType = typeof(ParsedFilterable<,>).MakeGenericType(root, rootReadModel, self, selfReadModel);

        if (rootKeyProperty == null || childKeyProperty == null || name == null)
        {
            parsed = Activator.CreateInstance(parsedType);
        }
        else
        {
            parsed = Activator.CreateInstance(parsedType, rootKeyProperty, childKeyProperty, name);
        }

        if (parsed is IParsedFilterable result)
        {
            return result;
        }
        throw new FilterParserException($"Could not parse {root.Name} -> {rootReadModel.Name}, {self.Name} -> {selfReadModel.Name}");
    }
}