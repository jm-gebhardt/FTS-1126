using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Gebhardt.Acc.Common.Filter.Enums;
using Gebhardt.Acc.Common.Filter.Exceptions;
using Gebhardt.Acc.Common.Filter.Expressions;
using Gebhardt.Acc.Common.Filter.FilterableParser.Attributes;
using Gebhardt.Acc.Common.Filter.FilterBuilder;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Common.Filter.FilterableParser;

/// <summary>
/// Binds <typeparamref name="TDomainObject"/> and <typeparamref name="TFilterable"/> for the purpose of creating database filter delegates.
/// </summary>
/// <typeparam name="TDomainObject">The domain object to be filtered.</typeparam>
/// <typeparam name="TFilterable">The read model for <typeparamref name="TDomainObject"/> that will be returned from the filter queries and describes what can be filtered.</typeparam>
[PublicAPI]
public sealed class FilterableBinder<TDomainObject, TFilterable> : FilterableBinderBase where TFilterable : class, TDomainObject, IFilterable<TDomainObject, TFilterable>, new() where TDomainObject : class
{
    private FilterableBinder()
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
    internal static readonly Dictionary<string, (LambdaExpression, bool)> PropertyAccessors = new();

    /// <summary>
    /// A dictionary containing the property accessors for the entity where the type in the overload of the read model matches the type in the domain model.
    /// </summary>
    // ReSharper disable once StaticMemberInGenericType // Intentional here, as every Filterable<TEntity> needs its own accessors.
    private static readonly Dictionary<string, (LambdaExpression, bool)> _domainPropertyAccessors = new();

    /// <summary>
    /// Dictionary of all the parsed filterables that contain join information.
    /// </summary>
    // ReSharper disable once StaticMemberInGenericType // Intentional here, as every Filterable<TEntity> needs its own join properties.
    private static readonly Dictionary<string, IParsedFilterable> _joinProperties = new();

    private static readonly ParsedFilterable<TDomainObject, TFilterable> _parsedFilterable = new();

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

    static FilterableBinder()
    {
        CreateAccessorMap();

        _includeLayers.Sort(static (s1, s2) => s1.Count(static c => c == '.').CompareTo(s2.Count(static c => c == '.')));
        _joinLayers.Sort(static (s1, s2) => s1.Count(static c => c == '.').CompareTo(s2.Count(static c => c == '.')));

        _joinLayers.Insert(0, IFilter.ReadModelFilterLayer);

        _totalLayers = _includeLayers.Concat(_joinLayers).ToList();
    }

    private static void CreateAccessorMap()
    {
        // Empty string always self-references the base entity by convention.
        PropertyAccessors.Add(IFilter.DomainFilterLayer, (ExpressionHelper.CreateIdentityExpression<TFilterable>(), false));
        _domainPropertyAccessors.Add(IFilter.DomainFilterLayer, (ExpressionHelper.CreateIdentityExpression<TDomainObject>(), false));

        const string accumulatedName = IFilter.DomainFilterLayer;

        CreateAccessorMap(_parsedFilterable, accumulatedName, true);

        CreateProjections(_parsedFilterable);

        CreateKeyAccessors();
    }

    private static void CreateKeyAccessors()
    {
        foreach (string key in _keyProperties)
        {
            if (PropertyAccessors.TryGetValue(key, out var accessorInfo))
            {
                _keyAccessors.Add(key, accessorInfo.Item1);
            }
            else
            {
                var type = typeof(TFilterable).GetProperty(key)?.PropertyType;

                if (type == null)
                {
                    throw new FilterParserException($"Could not create type for {key}");
                }

                var accessor = FilterExpressionBuilder.CreatePropertyAccess(typeof(TFilterable), type, key);

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
        string extendedAccumulatedName = accumulatedName + (isRoot ? "" : IFilter.AccessLevelSeparator);

        foreach (var leaf in parsedFilterable.Leaves.Concat(parsedFilterable.CollectionLeaves))
        {
            var accessor = (leaf.AccessorExpression, false);

            if (leaf is not IParsedFilterableCollectionLeaf)
            {
                accessor = TryMergeAccessors(accumulatedName, accessor.AccessorExpression, PropertyAccessors);
            }

            PropertyAccessors.Add(extendedAccumulatedName + leaf.Name, accessor);

            if (leaf.DomainAccessorExpression == null || !canStillAccessDomain)
            {
                continue;
            }

            var domAccessor = (leaf.DomainAccessorExpression, false);

            if (leaf is not IParsedFilterableCollectionLeaf)
            {
                domAccessor = TryMergeAccessors(accumulatedName, domAccessor.DomainAccessorExpression, _domainPropertyAccessors);
            }

            _domainPropertyAccessors.Add(extendedAccumulatedName + leaf.Name, domAccessor);
        }

        foreach (var parsedFilterableSubTree in parsedFilterable.SubTrees.Concat(parsedFilterable.CollectionSubTrees))
        {
            string newName = extendedAccumulatedName + parsedFilterableSubTree.Name;

            var accessor = (parsedFilterableSubTree.AccessorExpression, parsedFilterableSubTree.MaybeNull);

            if (parsedFilterableSubTree is not IParsedFilterableCollectionSubTree)
            {
                accessor = TryMergeAccessors(accumulatedName, accessor.AccessorExpression, PropertyAccessors, accessor.MaybeNull);
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
                var domAccessor = (parsedFilterableSubTree.DomainAccessorExpression, parsedFilterableSubTree.MaybeNull);

                if (parsedFilterableSubTree is not IParsedFilterableCollectionSubTree)
                {
                    domAccessor = TryMergeAccessors(accumulatedName, domAccessor.DomainAccessorExpression, _domainPropertyAccessors, domAccessor.MaybeNull);
                }

                _domainPropertyAccessors.Add(newName, domAccessor);
            }

            CreateAccessorMap(parsedFilterableSubTree.SubTree, newName, canStillAccessDomain && parsedFilterableSubTree.DomainAccessorExpression != null);
        }
    }

    private static (LambdaExpression, bool) TryMergeAccessors(string accumulatedName, LambdaExpression accessor, Dictionary<string, (LambdaExpression, bool)> accessors, bool accessorMaybeNull = false)
    {
        if (!accessors.TryGetValue(accumulatedName, out var accumulatedInfo) || accumulatedInfo is not { Item1 : {} accumulatedAccessor, Item2: var returnMaybeNull } || !accumulatedAccessor.ReturnType.IsAssignableTo(accessor.Parameters.First().Type))
        {
            return (accessor, false);
        }

        if (returnMaybeNull)
        {
            var returnType = accessor.ReturnType;
            if (accessor.ReturnType.IsValueType)
            {
                returnType = typeof(Nullable<>).MakeGenericType(returnType);
            }

            accessor = FilterExpressionBuilder.MakeNullCoalescing(accessor.Parameters.First(), accessor.Body, returnType);
        }

        return (ExpressionHelper.Compose(accumulatedAccessor, accessor), returnMaybeNull || accessorMaybeNull);
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

        var newProjection = TypeInitializerExpressionBuilder.CreateMemberInitializerExpression(domModel, readModel, TypeMappings, filterable.MemberMappings, filterable.UnmappedMembers);

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
    public delegate Task<ICollection<TFilterable>> FilterBy(IFilter filter);

    /// <summary>
    /// Filters an entity by the provided filter with pagination and sorting.
    /// </summary>
    /// <param name="filter">The filter.</param>
    /// <param name="pageNr">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="ordering">The direction to sort.</param>
    /// <param name="orderByProperties">The properties to sort.</param>
    public delegate Task<ICollection<TFilterable>> FilterByPaginatedWithOrdering(IFilter filter, int? pageNr, int? pageSize, SortDirection? ordering, ICollection<string> orderByProperties);

    private delegate IQueryable<TFilterable> ApplyFiltering<in TDbContext>(TDbContext context, IFilter filter);

    private static ApplyFiltering<TDbContext> CreateFilterApplier<TDbContext>() where TDbContext : DbContext
    {
        return (context, filterBase) =>
        {
            try
            {
                Dictionary<int, IFilter> batches = filterBase.SplitFilter(_totalLayers, _domainPropertyAccessors);

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

                if (!TryGetTypeMapping(typeof(TDomainObject), typeof(TFilterable), out var projection))
                {
                    throw new FilterExecutionException($"Could not find projection for type {typeof(TDomainObject).FullName}");
                }

                IQueryable<TFilterable> projectedQuery = query.ApplyProjection<TFilterable, TDomainObject>(projection);

                if (batches.TryGetValue(_includeLayers.Count, out var readModelBase))
                {
                    projectedQuery = projectedQuery.Where(readModelBase.ToExpressionTree(PropertyAccessors).AsPredicate<TFilterable>());
                }

                for (int layerIndex = _includeLayers.Count + 1; layerIndex < _totalLayers.Count; layerIndex++)
                {
                    if (_joinProperties[_totalLayers[layerIndex]] is not
                        {
                            QueryableCreator: { } queryableCreator,
                            RootKeyAccessor: { } ownKeyAccessor,
                            OtherKeyAccessor: { } otherKeyAccessor,
                            MergeExpression: { } mergeExpression,
                            IsOuterJoin: var isOuterJoin
                        })
                    {
                        throw new FilterExecutionException($"Could not create join for {_totalLayers[layerIndex]}");
                    }

                    projectedQuery = projectedQuery.ApplyJoin(queryableCreator.Invoke(context), ownKeyAccessor, otherKeyAccessor, mergeExpression, isOuterJoin);

                    if (batches.TryGetValue(layerIndex, out var predicate))
                    {
                        projectedQuery = projectedQuery.Where(predicate.ToExpressionTree(PropertyAccessors).AsPredicate<TFilterable>());
                    }
                }

                return projectedQuery;
            }
            catch (TargetInvocationException ex)
            {
                throw ex.GetBaseExceptionKeepingStacktrace();
            }
        };
    }
    
    /// <summary>
    /// Creates a filter for the entity defined by the attributes <see cref="FilterProperty"/>, <see cref="JoinProperty"/>, <see cref="NavigationProperty"/> and <see cref="KeyProperty"/>
    /// </summary>
    /// <typeparam name="TDbContext">The db context type.</typeparam>
    /// <param name="contextFactory">The db context factory</param>
    /// <returns>The filter.</returns>
    [RequiresUnreferencedCode("Requires attributes on the TReadModel type definition that describe how the read model projects the entity (See base class).")]
    public static FilterBy CreateFilter<TDbContext>(IDbContextFactory<TDbContext> contextFactory) where TDbContext : DbContext
    {
        ApplyFiltering<TDbContext> filterer = CreateFilterApplier<TDbContext>();

        return async delegate (IFilter filter)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            return await filterer(context, filter).ToListAsync();
        };
    }

    /// <summary>
    /// Creates a filter for the entity defined by the attributes <see cref="FilterProperty"/>, <see cref="JoinProperty"/>, <see cref="NavigationProperty"/> and <see cref="KeyProperty"/>
    /// </summary>
    /// <typeparam name="TDbContext">The db context type.</typeparam>
    /// <param name="contextFactory">The db context factory</param>
    /// <returns>The filter.</returns>
    [RequiresUnreferencedCode("Requires attributes on the TReadModel type definition that describe how the read model projects the entity (See base class).")]
    public static FilterByPaginatedWithOrdering CreateFilterWithPagination<TDbContext>(IDbContextFactory<TDbContext> contextFactory) where TDbContext : DbContext
    {
        ApplyFiltering<TDbContext> filterer = CreateFilterApplier<TDbContext>();

        return async delegate (IFilter filter, int? pageNr, int? pageSize, SortDirection? ordering, ICollection<string> orderByProperties) 
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            IQueryable<TFilterable> projectedQuery = filterer(context, filter);
            try
            {
                IOrderedQueryable<TFilterable>? orderedQuery = null;

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
                        orderedQuery = projectedQuery.OrderByAccessor(accessor.Item1, ordering);
                        continue;
                    }
                    orderedQuery = orderedQuery.ThenOrderByAccessor(accessor.Item1);
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

                projectedQuery = orderedQuery as IQueryable<TFilterable> ?? projectedQuery;

                bool usePagination = pageNr != null && pageSize != null;
                if (usePagination)
                {
                    projectedQuery = projectedQuery.Skip(pageNr!.Value * pageSize!.Value)
                        .Take(pageSize.Value);
                }

                return await projectedQuery.ToListAsync();
            }
            catch (TargetInvocationException ex)
            {
                throw ex.GetBaseExceptionKeepingStacktrace();
            }
        };
    }
    
    private static List<Tuple<PropertyInfo, PropertyInfo>> GetShadowingProperties()
    {
        PropertyInfo[] shadowing = typeof(TFilterable).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
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

    private static List<Tuple<PropertyInfo, PropertyInfo>> GetLeafProperties()
    {
        return GetShadowingProperties()
            .Where(props => 
                Attribute.IsDefined(props.Item2, typeof(FilterProperty)) &&
                !Attribute.IsDefined(props.Item2, typeof(InternalSubProperty)) && 
                !Attribute.IsDefined(props.Item2, typeof(HasConversion)))
            .ToList();
    }

    private static List<Tuple<PropertyInfo, PropertyInfo>> GetSubTreeProperties()
    {
        return GetShadowingProperties()
            .Where(props => 
                    Attribute.IsDefined(props.Item2, typeof(InternalSubProperty)) &&
                    !Attribute.IsDefined(props.Item2, typeof(HasConversion)))
            .ToList();
    }

    private static List<Tuple<PropertyInfo, PropertyInfo>> GetPropertiesWithConversion()
    {
        return GetShadowingProperties()
            .Where(props => Attribute.IsDefined(props.Item2, typeof(HasConversion)))
            .ToList();
    }

    /// <summary>
    /// Parsed version of this Filterable.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TF">The filterable type.</typeparam>
    private class ParsedFilterable<T, TF> : IParsedFilterable where TF : class, T, IFilterable<T, TF>, new() where  T : class
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

        public bool IsOuterJoin { get; }

        public ICollection<string> UnmappedMembers { get; } = new HashSet<string>();

        public ParsedFilterable(string ownKey, string otherKey, string name, bool isOuterJoin) : this(true)
        {
            IsOuterJoin = isOuterJoin;

            var ownKeyType = typeof(TFilterable).GetProperty(ownKey)?.PropertyType;
            var otherKeyType = typeof(TF).GetProperty(otherKey)?.PropertyType;

            if (otherKeyType == null || ownKeyType == null)
            {
                return;
            }

            bool ownKeyIsNullable = otherKeyType == Nullable.GetUnderlyingType(ownKeyType) && otherKeyType != ownKeyType;

            if (otherKeyType != ownKeyType && otherKeyType != Nullable.GetUnderlyingType(ownKeyType))
            {
                return;
            }

            RootKeyAccessor = FilterExpressionBuilder.CreatePropertyAccess(typeof(TFilterable), ownKeyType, ownKey);

            OtherKeyAccessor = ownKeyIsNullable ? 
                FilterExpressionBuilder.CreatePropertyAccessWithCast(typeof(T), ownKeyType, otherKey) : 
                FilterExpressionBuilder.CreatePropertyAccess(typeof(T), otherKeyType, otherKey);
            
            QueryableCreator = context => context.Set<T>().AsNoTracking();
            
            if (!TryGetTypeMapping(typeof(T), typeof(TF), out var memberInitializer))
            {
                memberInitializer = TypeInitializerExpressionBuilder.CreateMemberInitializerExpression<T, TF>(TypeMappings, MemberMappings, UnmappedMembers);
            }

            if (IsOuterJoin)
            {
                memberInitializer = FilterExpressionBuilder.MakeNullCoalescing(memberInitializer.Parameters.First(), memberInitializer.Body, typeof(TF));
            }
            MergeExpression = TypeInitializerExpressionBuilder.CreateInitializerWithMixin<TFilterable, T>(name, memberInitializer);
        }

        private ParsedFilterable(bool isJoin)
        {
            ExtractConvertedProperties();

            ExtractLeaves(isJoin);

            ExtractSubTrees(isJoin);
        }

        private void ExtractConvertedProperties()
        {
            List<Tuple<PropertyInfo, PropertyInfo>> conversionProperties = FilterableBinder<T, TF>.GetPropertiesWithConversion();

            foreach (var (originalProp , newProp) in conversionProperties)
            {
                if (originalProp.PropertyType != newProp.PropertyType)
                {
                    continue;
                }

                UnmappedMembers.Add(originalProp.Name);
            }
        }

        public ParsedFilterable() : this(false)
        {
        }

        private void ExtractSubTrees(bool isJoin)
        {
            List<Tuple<PropertyInfo, PropertyInfo>> subProperties = FilterableBinder<T,TF>.GetSubTreeProperties();

            foreach (var (originalProp, newProperty) in subProperties)
            {
                var newBasePropType = newProperty.PropertyType.GetEnumerableContentType() ?? newProperty.PropertyType;
                var originalBasePropType = originalProp.PropertyType.GetEnumerableContentType() ?? originalProp.PropertyType;
                
                bool canAccessDomainObject = !isJoin;

                if (!typeof(IFilterable).IsAssignableFrom(newBasePropType))
                {
                    continue;
                }

                var accessor = FilterExpressionBuilder.CreatePropertyAccess(typeof(TF), newProperty.PropertyType, newProperty.Name);

                var domainAccessor = canAccessDomainObject ? FilterExpressionBuilder.CreatePropertyAccess(typeof(T), originalProp.PropertyType, newProperty.Name) : null;

                IParsedFilterable subTree;

                if (Attribute.GetCustomAttribute(newProperty, typeof(JoinProperty)) is JoinProperty joinProperty)
                {
                    if (newBasePropType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IFilterable<,>))?.GenericTypeArguments.FirstOrDefault() is not { } originalDomainModel)
                    {
                        throw new FilterParserException($"Could not parse property {newProperty.Name} for filterable {typeof(TF).FullName}");
                    }

                    subTree = ParsedFilterableFromTypes(typeof(T), typeof(TF), originalDomainModel, newBasePropType, joinProperty.OwnKeyProperty, joinProperty.OtherKeyProperty, newProperty.Name, joinProperty.IsOuterJoin);

                    originalBasePropType = originalDomainModel;

                    MemberMappings.Add(joinProperty.OwnKeyProperty, newProperty.Name);
                }
                else
                {
                    subTree = ParsedFilterableFromTypes(typeof(T), typeof(TF), originalBasePropType, newBasePropType);
                }

                if (newBasePropType != newProperty.PropertyType)
                {
                    CollectionSubTrees.Add(CollectionSubTreeFromTypes(typeof(T), typeof(TF), newProperty.PropertyType, originalBasePropType, newBasePropType, newProperty.Name, accessor, subTree, newProperty.MemberMaybeNull(), domainAccessor));
                }
                else
                {
                    SubTrees.Add(SubTreeFromTypes(typeof(T), typeof(TF), originalBasePropType, newBasePropType, newProperty.Name, accessor, subTree, newProperty.MemberMaybeNull(), domainAccessor));
                }
            }
        }

        private void ExtractLeaves(bool isJoin)
        {
            List<Tuple<PropertyInfo, PropertyInfo>> filterProperties = FilterableBinder<T, TF>.GetLeafProperties();

            foreach (var (oldProperty, newProperty) in filterProperties)
            {
                var accessor = FilterExpressionBuilder.CreatePropertyAccess(typeof(TF), newProperty.PropertyType, newProperty.Name);

                bool canAccessDomainObject = newProperty.PropertyType == oldProperty.PropertyType && !isJoin;

                var domainAccessor = canAccessDomainObject ? FilterExpressionBuilder.CreatePropertyAccess(typeof(T), newProperty.PropertyType, newProperty.Name) : null;

                var newCollectionType = newProperty.PropertyType.GetEnumerableContentType();

                if (newCollectionType != null)
                {
                    CollectionLeaves.Add(CollectionLeafFromTypes(typeof(T), typeof(TF), newProperty.PropertyType, newCollectionType, newProperty.Name, accessor, domainAccessor));
                }
                else
                {
                    Leaves.Add(LeafFromTypes(typeof(T), typeof(TF), newProperty.PropertyType, newProperty.Name, accessor, domainAccessor));
                }
            }
        }
    }

    private static IParsedFilterable ParsedFilterableFromTypes(Type root, Type rootReadModel, Type self, Type selfReadModel, string? rootKeyProperty = null, string? childKeyProperty = null, string? name = null, bool? isOuterJoin = null)
    {
        object? parsed;

        var parsedType = typeof(ParsedFilterable<,>).MakeGenericType(root, rootReadModel, self, selfReadModel);

        if (rootKeyProperty == null || childKeyProperty == null || name == null || isOuterJoin == null)
        {
            parsed = Activator.CreateInstance(parsedType);
        }
        else
        {
            parsed = Activator.CreateInstance(parsedType, rootKeyProperty, childKeyProperty, name, isOuterJoin);
        }

        if (parsed is IParsedFilterable result)
        {
            return result;
        }
        throw new FilterParserException($"Could not parse {root.Name} -> {rootReadModel.Name}, {self.Name} -> {selfReadModel.Name}");
    }
}