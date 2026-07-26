using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace ClinicHub.Services.ReponseModels
{
    public class PagginatedResult<T>
    {
        public const int DefaultPageNumber = 1;
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;

        [JsonPropertyName("items")]
        public IReadOnlyCollection<T> Items { get; }

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; }

        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage => PageNumber > 1;

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage => PageNumber < TotalPages;

        [Newtonsoft.Json.JsonConstructor]
        public PagginatedResult(
            [JsonProperty("items")] IReadOnlyCollection<T> items,
            [JsonProperty("totalCount")] int count,
            [JsonProperty("pageNumber")] int pageNumber = DefaultPageNumber,
            [JsonProperty("pageSize")] int pageSize = DefaultPageSize)
        {
            PageNumber = pageNumber < 1 ? DefaultPageNumber : pageNumber;
            PageSize = pageSize < 1 ? DefaultPageSize : pageSize > MaxPageSize ? MaxPageSize : pageSize;
            TotalPages = PageSize > 0 ? (int)Math.Ceiling(count / (double)PageSize) : 0;
            TotalCount = count;
            Items = items;
        }

        public static PagginatedResult<T> Create(IReadOnlyCollection<T> items, int count, int pageNumber = DefaultPageNumber, int pageSize = DefaultPageSize)
            => new(items, count, pageNumber, pageSize);
    }
}
