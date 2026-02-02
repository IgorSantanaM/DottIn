namespace DottIn.Domain.Common
{
    public class PagedResult<T> where T : class
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

    }
}
