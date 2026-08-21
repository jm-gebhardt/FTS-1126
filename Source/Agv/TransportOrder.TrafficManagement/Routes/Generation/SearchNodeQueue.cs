using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Routes.Generation;

/// <summary>
/// A priority queue for managing search nodes during route generation, performing domination check on enqueueing.
/// </summary>
internal class SearchNodeQueue
{
    private readonly SearchNodeComparer _searchNodeComparer;

    private readonly PriorityQueue<SearchNode, double> _priorityQueue;

    private readonly HashSet<SearchNode> _dominatingSearchNodes;

    /// <summary>
    /// Gets the total number of search nodes that have been enqueued, including those that were not added due to domination checks.
    /// </summary>
    public int EnqueuedCount { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchNodeQueue"/> class.
    /// </summary>
    public SearchNodeQueue()
    {
        _searchNodeComparer = new SearchNodeComparer();
        _priorityQueue = new PriorityQueue<SearchNode, double>();
        _dominatingSearchNodes = new HashSet<SearchNode>(_searchNodeComparer);
    }

    /// <summary>
    /// Attempts to enqueue a search node into the priority queue. If a dominating search node already exists, the new node is not enqueued.
    /// </summary>
    /// <param name="searchNode">The search node to enqueue.</param>
    /// <returns>True if the search node was enqueued; false if a dominating search node already exists.</returns>
    public bool TryEnqueue(SearchNode searchNode)
    {
        EnqueuedCount++;

        // Check if a dominating search node already enqueued or processed
        if (_dominatingSearchNodes.TryGetValue(searchNode, out var existingSearchNode))
        {
            Debug.Assert(!ReferenceEquals(searchNode, existingSearchNode));

            if (_searchNodeComparer.FirstDominatesSecond(existingSearchNode, searchNode))
            {
                // existingSearchNode dominates searchNode, so we don't enqueue the new searchNode
                return false;
            }

            // Remove the existing dominated search node
            _priorityQueue.Remove(existingSearchNode, out _, out _);
            _dominatingSearchNodes.Remove(existingSearchNode);
        }

        // Enqueue the new dominating search node
        _priorityQueue.Enqueue(searchNode, searchNode.TotalCost);
        _dominatingSearchNodes.Add(searchNode);
        return true;
    }

    /// <summary>
    /// Attempts to dequeue the search node with the lowest cost from the priority queue.
    /// </summary>
    /// <param name="searchNode">The search node with the lowest cost, if available.</param>
    /// <returns>True if a search node was dequeued; false if the queue is empty.</returns>
    public bool TryDequeue([MaybeNullWhen(false)] out SearchNode searchNode)
    {
        return _priorityQueue.TryDequeue(out searchNode, out _);
    }
}