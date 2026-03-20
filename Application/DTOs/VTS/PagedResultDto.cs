using System.Collections.Generic;

namespace Application.DTOs
{
    /// <summary>
    /// Generic paged result wrapper.
    /// </summary>
    public class PagedResultDto<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
