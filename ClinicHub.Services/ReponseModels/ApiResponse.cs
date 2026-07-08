namespace ClinicHub.Services.ReponseModels
{
    public record ApiResponse<TData>
    {
        public bool Success { get; set; }
        public IDictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
        public TData? Data { get; set; }
        public string? Message { get; set; }
        public int StatusCode { get; set; }
    }
}
