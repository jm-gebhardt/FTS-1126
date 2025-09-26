using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Gebhardt.Acc.Common.Filter.Exceptions;
using Gebhardt.Acc.Common.Filter.Expressions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.Common.Filter.FilterableParser;

/// <summary>
/// Holds static properties common to all variants of <see cref="FilterableBinder{TDomainObject,TReadModel}"/>.
/// </summary>
[PublicAPI]
public abstract class FilterableBinderBase
{
    private static readonly ConcurrentDictionary<Tuple<Type, Type>, LambdaExpression> _typeMappings = new();

    /// <summary>
    /// Type mappings defined for all <see cref="IFilterable{TEntity,TReadModel}"/> pairings.
    /// </summary>
    protected static IReadOnlyDictionary<Tuple<Type, Type>, LambdaExpression> TypeMappings => _typeMappings;

    /// <summary>
    /// Tries to get a type mapping for the provided types.
    /// </summary>
    /// <param name="from">First type.</param>
    /// <param name="to">Second type.</param>
    /// <param name="result">The type mapping <see cref="LambdaExpression"/> if it exists.</param>
    /// <returns>true if the binding exists, false otherwise.</returns>
    protected static bool TryGetTypeMapping(Type from, Type to, [MaybeNullWhen(false)] out LambdaExpression result) 
    {
        if (from == to)
        {
            throw new FilterParserException($"Filtering is only supported for explicit read models: {from.FullName}");
        }
        return _typeMappings.TryGetValue(new Tuple<Type, Type>(from, to), out result);
    }

    /// <summary>
    /// Tries to add a type mapping.
    /// </summary>
    /// <param name="from">First type.</param>
    /// <param name="to">Second type.</param>
    /// <param name="mapping">The mapping to get from one type to the other.</param>
    protected static bool TryAddTypeMapping(Type from, Type to, LambdaExpression mapping) => _typeMappings.TryAdd(new Tuple<Type, Type>(from, to), mapping);

    /// <summary>
    /// Parsed leaf property.
    /// </summary>
    protected interface IParsedFilterableLeaf
    {
        /// <summary>
        /// Name of the leaf property.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Accessor to the leaf property from the root.
        /// </summary>
        LambdaExpression AccessorExpression { get; }

        /// <summary>
        /// Accessor to the leaf property from the domain object if the types match.
        /// </summary>
        LambdaExpression? DomainAccessorExpression { get; }
    }

    /// <summary>
    /// Parsed leaf property with a collection type.
    /// </summary>
    protected interface IParsedFilterableCollectionLeaf : IParsedFilterableLeaf;

    /// <summary>
    /// Parsed subtree with a collection type.
    /// </summary>
    protected interface IParsedFilterableCollectionSubTree : IParsedFilterableSubTree;

    /// <summary>
    /// Parsed subtree.
    /// </summary>
    protected interface IParsedFilterableSubTree
    {
        /// <summary>
        /// Name of the subtree property.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Accessor from the root to the subtree.
        /// </summary>
        LambdaExpression AccessorExpression { get; }

        /// <summary>
        /// Accessor from the domain root to the subtree if the types match.
        /// </summary>
        LambdaExpression? DomainAccessorExpression { get; }

        /// <summary>
        /// The subtree contents.
        /// </summary>
        IParsedFilterable SubTree { get; }

        /// <summary>
        /// Whether this property is declared as nullable.
        /// </summary>
        bool MaybeNull { get; }
    }

    /// <summary>
    /// A parsed read model.
    /// </summary>
    protected interface IParsedFilterable
    {
        /// <summary>
        /// All the leaves on the read model.
        /// </summary>
        List<IParsedFilterableLeaf> Leaves { get; }

        /// <summary>
        /// All the leaves on the read model that have a collection type.
        /// </summary>
        List<IParsedFilterableCollectionLeaf> CollectionLeaves { get; }

        /// <summary>
        /// All the subtrees of the read model.
        /// </summary>
        List<IParsedFilterableSubTree> SubTrees { get; }

        /// <summary>
        /// All the subtrees of the read model that have a collection type.
        /// </summary>
        List<IParsedFilterableCollectionSubTree> CollectionSubTrees { get; }

        /// <summary>
        /// Mapping for members from the domain to the read model where not implicitly given by the same name.
        /// Usually all the foreign keys.
        /// </summary>
        Dictionary<string, string> MemberMappings { get; }

        /// <summary>
        /// If this read model is the other side of a join.
        /// </summary>
        bool IsJoin { get; }

        /// <summary>
        /// Accessor to the key on the previous level.
        /// </summary>
        LambdaExpression? RootKeyAccessor { get; }

        /// <summary>
        /// Accessor to the key on this level.
        /// </summary>
        LambdaExpression? OtherKeyAccessor { get; }

        /// <summary>
        /// Creator for the queryable to join with.
        /// </summary>
        Func<DbContext, IQueryable>? QueryableCreator { get; }

        /// <summary>
        /// Expression to merge root with a value from this level.
        /// </summary>
        LambdaExpression? MergeExpression { get; }

        /// <summary>
        /// Whether the join should be LEFT OUTER or not.
        /// </summary>
        bool IsOuterJoin { get; }

        /// <summary>
        /// Collection of members that will not be mapped.
        /// </summary>
        ICollection<string> UnmappedMembers { get; }
    }

    /// <summary>
    /// Leaf of a parsed <see cref="FilterableBinder{TDomainObject,TReadModel}"/>
    /// </summary>
    /// <typeparam name="TRoot">The root type of the domain model.</typeparam>
    /// <typeparam name="TSelf">The type of the leaf property</typeparam>
    /// <typeparam name="TRootReadModel">The type of the read model.</typeparam>
    /// <param name="name">The name of the property.</param>
    /// <param name="accessorExpression">An expression to reach this property.</param>
    /// <param name="domainAccessorExpression">An expression to reach this property.</param>
    private class ParsedFilterableLeaf<TRoot, TRootReadModel, TSelf>(string name, Expression<Func<TRootReadModel, TSelf>> accessorExpression, Expression<Func<TRoot, TSelf>>? domainAccessorExpression = null) : IParsedFilterableLeaf
        where TRoot : class
        where TRootReadModel : class, TRoot
    {
        /// <inheritdoc />
        public string Name => name;

        /// <inheritdoc />
        public LambdaExpression AccessorExpression => accessorExpression;

        /// <inheritdoc />
        public LambdaExpression? DomainAccessorExpression => domainAccessorExpression;
    }
    
    /// <summary>
    /// Leaf of a parsed <see cref="FilterableBinder{TDomainObject,TReadModel}"/>
    /// </summary>
    /// <typeparam name="TRoot">The root type.</typeparam>
    /// <typeparam name="TCollection">Intermediary type to represent a collection of <typeparamref name="TSelf"/>.</typeparam>
    /// <typeparam name="TSelf">The type of the leaf property.</typeparam>
    /// <typeparam name="TRootReadModel">Read model of the root.</typeparam>
    /// <param name="name">The name of the property.</param>
    /// <param name="accessorExpression">An expression to reach the collection of this property.</param>
    /// <param name="domainAccessorExpression">An expression to reach the collection of this property.</param>
    private class ParsedFilterableCollectionLeaf<TRoot, TRootReadModel, TCollection, TSelf>(string name, Expression<Func<TRootReadModel, TCollection>> accessorExpression, Expression<Func<TRoot, TCollection>>? domainAccessorExpression = null) : IParsedFilterableCollectionLeaf
        where TRoot : class
        where TRootReadModel : class
        where TSelf : IComparable<TSelf>
        where TCollection : IEnumerable<TSelf>
    {
        /// <inheritdoc />
        public string Name => name;

        /// <inheritdoc />
        public LambdaExpression AccessorExpression => accessorExpression;

        /// <inheritdoc />
        public LambdaExpression? DomainAccessorExpression => domainAccessorExpression;
    }

    /// <summary>
    /// Subtree of a <see cref="FilterableBinder{TDomainObject,TReadModel}"/> that is a collection.
    /// </summary>
    /// <typeparam name="TRoot">The root type.</typeparam>
    /// <typeparam name="TRmCollection">Intermediary type to represent a collection of <typeparamref name="TSelf"/>.</typeparam>
    /// <typeparam name="TSelf">The type of the collections contents.</typeparam>
    /// <typeparam name="TSelfReadModel"></typeparam>
    /// <typeparam name="TRootReadModel"></typeparam>
    /// <param name="name">The name of this sub-property</param>
    /// <param name="accessorExpression">An expression to reach the collection of this sub-property.</param>
    /// <param name="domainAccessorExpression">An expression to reach the collection of this sub-property.</param>
    /// <param name="subTree">The subtree.</param>
    /// <param name="maybeNull">If this subtree was declared as nullable.</param>
    private class ParsedFilterableCollectionSubTree<TRoot, TRootReadModel, TRmCollection, TSelf, TSelfReadModel>(string name, Expression<Func<TRootReadModel, TRmCollection>> accessorExpression, IParsedFilterable subTree, bool maybeNull, LambdaExpression? domainAccessorExpression = null) : IParsedFilterableCollectionSubTree
        where TRoot : class
        where TRootReadModel : class
        where TSelfReadModel : class, IFilterable<TSelf, TSelfReadModel>, TSelf, new()
        where TSelf : class
        where TRmCollection : IEnumerable<TSelfReadModel>
    {
        /// <inheritdoc />
        public string Name => name;

        /// <inheritdoc />
        public LambdaExpression AccessorExpression => accessorExpression;

        /// <inheritdoc />
        public LambdaExpression? DomainAccessorExpression => domainAccessorExpression;

        /// <inheritdoc />
        public IParsedFilterable SubTree => subTree;

        /// <inheritdoc />>
        public bool MaybeNull => maybeNull;
    }

    /// <summary>
    /// Subtree of a <see cref="FilterableBinder{TDomainObject,TReadModel}"/>.
    /// </summary>
    /// <typeparam name="TRoot">The root type.</typeparam>
    /// <typeparam name="TSelf">The type of this sub-property.</typeparam>
    /// <typeparam name="TSelfReadModel"></typeparam>
    /// <typeparam name="TRootReadModel"></typeparam>
    /// <param name="name">The name of this property.</param>
    /// <param name="accessorExpression">An expression to reach this sub-property.</param>
    /// <param name="domainAccessorExpression">An expression to reach this sub-property.</param>
    /// <param name="subTree">The subtree.</param>
    /// <param name="maybeNull">If this subtree was declared as nullable.</param>
    private class ParsedFilterableSubTree<TRoot, TRootReadModel, TSelf, TSelfReadModel>(string name, Expression<Func<TRootReadModel, TSelfReadModel>> accessorExpression, IParsedFilterable subTree, bool maybeNull, LambdaExpression? domainAccessorExpression = null) : IParsedFilterableSubTree
        where TRoot : class
        where TRootReadModel : class
        where TSelfReadModel : class, IFilterable<TSelf, TSelfReadModel>, TSelf, new()
        where TSelf : class
    {
        /// <inheritdoc />
        public string Name => name;

        /// <inheritdoc />
        public LambdaExpression AccessorExpression => accessorExpression;

        /// <inheritdoc />
        public LambdaExpression? DomainAccessorExpression => domainAccessorExpression;

        /// <inheritdoc />
        public IParsedFilterable SubTree => subTree;

        /// <inheritdoc />
        public bool MaybeNull => maybeNull;
    }

    /// <summary>
    /// Creates a parsed leaf from materialized types.
    /// </summary>
    /// <param name="root">The root type.</param>
    /// <param name="rootReadModel">The read model type of the root.</param>
    /// <param name="self">Own type.</param>
    /// <param name="name">Name of the property.</param>
    /// <param name="accessor">Accessor from root.</param>
    /// <param name="domainAccessor">Accessor from the domain root if the types match.</param>
    protected static IParsedFilterableLeaf LeafFromTypes(Type root, Type rootReadModel, Type self, string name, LambdaExpression accessor, LambdaExpression? domainAccessor = null)
    {
        if (!rootReadModel.IsAssignableTo(root))
        {
            throw new FilterParserException($"Could not parse {name}, {rootReadModel.Name} is not a read model of {root.Name}");
        }

        var leafType = typeof(ParsedFilterableLeaf<,,>).MakeGenericType(root, rootReadModel, self);

        if (Activator.CreateInstance(leafType, name, accessor, domainAccessor) is IParsedFilterableLeaf result)
        {
            return result;
        }
        throw new FilterParserException($"Could not parse leaf {root.Name}, {name}");
    }

    /// <summary>
    /// Creates a parsed leaf with a collection type from materialized types.
    /// </summary>
    /// <param name="root">The root type.</param>
    /// <param name="rootReadModel">The read model type of the root.</param>
    /// <param name="collection">The collection type.</param>
    /// <param name="self">Own type.</param>
    /// <param name="name">Name of the property.</param>
    /// <param name="accessor">Accessor from root.</param>
    /// <param name="domainAccessor">Accessor from the domain root if the types match.</param>
    protected static IParsedFilterableCollectionLeaf CollectionLeafFromTypes(Type root, Type rootReadModel, Type collection, Type self, string name, LambdaExpression accessor, LambdaExpression? domainAccessor = null)
    {
        if (!rootReadModel.IsAssignableTo(root))
        {
            throw new FilterParserException($"Could not parse {name}, {rootReadModel.Name} is not a read model of {root.Name}");
        }

        if (collection.GetEnumerableContentType() is not { } contentType || contentType != self)
        {
            throw new FilterParserException($"Could not parse {name}, {collection.Name} is not an enumerable type of {self.Name}");
        }

        var leafType = typeof(ParsedFilterableCollectionLeaf<,,,>).MakeGenericType(root, rootReadModel, collection, self);

        if (Activator.CreateInstance(leafType, name, accessor, domainAccessor) is IParsedFilterableCollectionLeaf result)
        {
            return result;
        }
        throw new FilterParserException($"Could not parse collection leaf {root.Name}, {name}");
    }

    /// <summary>
    /// Creates a parsed subtree from materialized types.
    /// </summary>
    /// <param name="root">The root type.</param>
    /// <param name="rootReadModel">The root read model type.</param>
    /// <param name="self">Own type.</param>
    /// <param name="selfReadModel">Own read model type.</param>
    /// <param name="name">Name of the property.</param>
    /// <param name="accessor">Accessor from root.</param>
    /// <param name="subTree">Subtree.</param>
    /// <param name="maybeNull">If this subtree is nullable.</param>
    /// <param name="domainAccessor">Accessor from domain root.</param>
    protected static IParsedFilterableSubTree SubTreeFromTypes(Type root, Type rootReadModel, Type self, Type selfReadModel, string name, LambdaExpression accessor, IParsedFilterable subTree, bool maybeNull, LambdaExpression? domainAccessor = null)
    {
        if (!rootReadModel.IsAssignableTo(root))
        {
            throw new FilterParserException($"Could not parse {name}, {rootReadModel.Name} is not a read model of {root.Name}");
        }

        if (!selfReadModel.IsAssignableTo(self))
        {
            throw new FilterParserException($"Could not parse {name}, {selfReadModel.Name} is not a read model of {self.Name}");
        }

        var subType = typeof(ParsedFilterableSubTree<,,,>).MakeGenericType(root, rootReadModel, self, selfReadModel);

        if (Activator.CreateInstance(subType, name, accessor, subTree, maybeNull, domainAccessor) is IParsedFilterableSubTree result)
        {
            return result;
        }
        throw new FilterParserException($"Could not create sub tree {root.Name}, {name}");
    }

    /// <summary>
    /// Creates a parsed subtree with a collection type from materialized types.
    /// </summary>
    /// <param name="root">The root type.</param>
    /// <param name="rootReadModel">The root read model type.</param>
    /// <param name="collection">The collection type.</param>
    /// <param name="self">Own type.</param>
    /// <param name="selfReadModel">Own read model type.</param>
    /// <param name="name">Name of the property.</param>
    /// <param name="accessor">Accessor from the read model.</param>
    /// <param name="subTree">The subtree.</param>
    /// <param name="maybeNull">If this subtree is nullable.</param>
    /// <param name="domainAccessor">The accessor from the domain object if the types match.</param>
    protected static IParsedFilterableCollectionSubTree CollectionSubTreeFromTypes(Type root, Type rootReadModel, Type collection, Type self, Type selfReadModel, string name, LambdaExpression accessor, IParsedFilterable subTree, bool maybeNull, LambdaExpression? domainAccessor = null)
    {
        if (!rootReadModel.IsAssignableTo(root))
        {
            throw new FilterParserException($"Could not parse {name}, {rootReadModel.Name} is not a read model of {root.Name}");
        }

        if (!selfReadModel.IsAssignableTo(self))
        {
            throw new FilterParserException($"Could not parse {name}, {selfReadModel.Name} is not a read model of {self.Name}");
        }

        if (collection.GetEnumerableContentType() is not { } contentType || contentType != selfReadModel)
        {
            throw new FilterParserException($"Could not parse {name}, {collection.Name} is not an enumerable type of {selfReadModel.Name}");
        }

        var subType = typeof(ParsedFilterableCollectionSubTree<,,,,>).MakeGenericType(root, rootReadModel, collection, self, selfReadModel);

        if (Activator.CreateInstance(subType, name, accessor, subTree, maybeNull, domainAccessor) is IParsedFilterableCollectionSubTree result)
        {
            return result;
        }
        throw new FilterParserException($"Could not parse collection sub tree {root.Name}, {name}");
    }

    /// <summary>
    /// Not supposed to be instantiated.
    /// </summary>
    internal FilterableBinderBase()
    {
    }
}