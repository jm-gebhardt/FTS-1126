using System.Collections;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers.Pagination
{
    /// <summary>
    /// Paged Enumerable
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagedEnumerable<T> : IEnumerable<T>
    {
        /// <summary>
        /// Current Page
        /// </summary>
        [UsedImplicitly]
        public int CurrentPage { get; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        [UsedImplicitly]
        public int TotalPages { get; }

        /// <summary>
        /// Size of the page
        /// </summary>
        [UsedImplicitly]
        public int PageSize { get; }

        /// <summary>
        /// Total number of items
        /// </summary>
        [UsedImplicitly]
        public int TotalCount { get; }

        /// <summary>
        /// Is there a previous page
        /// </summary>
        [UsedImplicitly]
        public bool HasPrevious => CurrentPage > 1;

        /// <summary>
        /// Is there a next page
        /// </summary>
        [UsedImplicitly]
        public bool HasNext => CurrentPage < TotalPages;

        private readonly IEnumerable<T> _items;

        /// <summary>
        /// PagedEnumerable Constructor
        /// </summary>
        /// <param name="source">Source list</param>
        /// <param name="pageNumber">Current page number</param>
        /// <param name="pageSize">Items per page</param>
        public PagedEnumerable(IQueryable<T> source, int pageNumber, int pageSize)
        {
            TotalCount = source.Count();
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(TotalCount / (double)pageSize);

            int start = (pageNumber - 1) * pageSize;
            _items = source.Skip(start).Take(pageSize);
        }
        
        /// <summary>
        /// Get Pagination headers
        /// </summary>
        /// <returns>PaginationHeaders</returns>
        public PaginationHeaders PaginationHeader()
        {
            return new PaginationHeaders(CurrentPage, TotalPages, PageSize, TotalCount, HasPrevious, HasNext);
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
