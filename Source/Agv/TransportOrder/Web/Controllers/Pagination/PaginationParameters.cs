using System.ComponentModel;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers.Pagination
{
   
    /// <summary>
    /// Query Parameters for paging 
    /// </summary>
    [PublicAPI]
    public class PaginationParameters
    {
        /// <summary>
        /// Default Page Size
        /// </summary>
        [UsedImplicitly]
        public const int DefaultPageSize = 25;
        
        /// <summary>
        /// Default Page Number
        /// </summary>
        [UsedImplicitly]
        public const int DefaultPageNumber = 1;
        
        private const int MaxPageSize = 100;
        
        /// <summary>
        /// Page Number
        /// </summary>
        [DefaultValue(DefaultPageNumber)]
        public int PageNumber { get; set; } = DefaultPageNumber;
        private int _pageSize = DefaultPageSize;
        /// <summary>
        /// Page Size
        /// </summary>
        [DefaultValue(DefaultPageSize)]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }
    }
}