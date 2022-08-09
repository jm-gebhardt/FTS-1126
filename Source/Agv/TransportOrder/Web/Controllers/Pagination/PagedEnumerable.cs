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
        /// <param name="items"></param>
        /// <param name="count"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        private PagedEnumerable(IEnumerable<T> items, int count, int pageNumber, int pageSize)
        {
            _items = items;
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        }

        /// <summary>
        /// Convert given queryable into pages List
        /// </summary>
        /// <param name="source"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static PagedEnumerable<T> ToPagedList(IQueryable<T> source, int pageNumber, int pageSize)
        {
            int count = source.Count();
            IEnumerable<T> items = source.Skip((pageNumber - 1) * pageSize).Take(pageSize).AsEnumerable();

            return new PagedEnumerable<T>(items, count, pageNumber, pageSize);
        }

        /// <summary>
        /// Get Pagination headers
        /// </summary>
        /// <returns>PaginationHeader</returns>
        public PaginationHeader PaginationHeader()
        {
            return new PaginationHeader(CurrentPage, TotalPages, PageSize, TotalCount, HasPrevious, HasNext);
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


