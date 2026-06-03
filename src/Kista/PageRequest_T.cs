namespace Kista
{
    public class PageRequest
    {
        public PageRequest(int page, int size) {
            ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);

            Page = page;
            Size = size;
        }
        /// <summary>
        /// Gets the number of the page to return
        /// </summary>
        public int Page { get; }

        /// <summary>
        /// Gets the maximum number of items to be returned.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Gets the starting offet in the repository where to start
        /// collecting the items to return
        /// </summary>
        public int Offset => (Page - 1) * Size;
    }
}