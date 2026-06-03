namespace Kista
{
    public class PageResult<TEntity> where TEntity : class
    {
        public PageResult(PageRequest request, int totalItems, IEnumerable<TEntity>? items = null) {
            if (totalItems < 0)
                throw new ArgumentOutOfRangeException(nameof(totalItems), "The number of total items must be zero or more");

            Request = request ?? throw new ArgumentNullException(nameof(request));
            TotalItems = totalItems;
            Items = items?.ToList().AsReadOnly();
        }
        
        /// <summary>
        /// Gets a reference to the request
        /// </summary>
        public PageRequest Request { get; }

        /// <summary>
        /// Gets a count of the total items in the repository
        /// for the context of the request
        /// </summary>
        public int TotalItems { get; }

        /// <summary>
        /// Gets a list of items included in the page
        /// </summary>
        public IReadOnlyList<TEntity>? Items { get; set; }

        /// <summary>
        /// Gets a count of the total available pages
        /// that can be requested from the repository
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / Request.Size);

        /// <summary>
        /// Gets a value indicating if the current page is the first
        /// in the context of the request
        /// </summary>
        public bool IsFirstPage => Request.Page == 1;

        /// <summary>
        /// Gets a value indicating if the current page is the last
        /// in the context of the request
        /// </summary>
        public bool IsLastPage => Request.Page == TotalPages;

        /// <summary>
        /// Gets a value indicating if there is a next page
        /// in the context of the request
        /// </summary>
        public bool HasNextPage => !IsLastPage;

        /// <summary>
        /// Gets a value indicating if there is a previous page
        /// in the context of the request
        /// </summary>
        public bool HasPreviousPage => !IsFirstPage;

        /// <summary>
        /// When there is a next page, gets the number of the next page
        /// in the context of the request, or <c>null</c> if there is no
        /// page after the current one.
        /// </summary>
        public int? NextPage => HasNextPage ? Request.Page + 1 : null;

        /// <summary>
        /// When there is a previous page, gets the number of the previous page
        /// in the context of the request, or <c>null</c> if there is no
        /// page before the current one.
        /// </summary>
        public int? PreviousPage => HasPreviousPage ? Request.Page - 1 : null;

    }
}