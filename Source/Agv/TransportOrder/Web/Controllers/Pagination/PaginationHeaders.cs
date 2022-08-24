using JetBrains.Annotations;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers.Pagination
{
    /// <summary>
    /// Pagination X Headers
    /// </summary>
    [PublicAPI]
    public record PaginationHeader
    {
        /// <summary>
        /// Header name
        /// </summary>
        public const string HeaderName = "X-Pagination";
        /// <summary>
        /// Current Page
        /// </summary>
        public int CurrentPage { get; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; }

        /// <summary>
        /// Size of the page
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Total number of items
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Is there a previous page
        /// </summary>
        public bool HasPrevious { get; }
        /// <summary>
        /// Is there a next page
        /// </summary>
        public bool HasNext { get; }

        /// <summary>
        /// Pagination X Headers Constructor
        /// </summary>
        public PaginationHeader(int currentPage, int totalPages, int pageSize, int totalCount, bool hasPrevious, bool hasNext)
        {
            CurrentPage = currentPage;
            TotalPages = totalPages;
            PageSize = pageSize;
            TotalCount = totalCount;
            HasPrevious = hasPrevious;
            HasNext = hasNext;
        }
    }
}
