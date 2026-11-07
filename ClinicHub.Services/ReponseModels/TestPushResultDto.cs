namespace ClinicHub.Services.ReponseModels
{
    public class TestPushResultDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = "";
        public int Type { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
        public string? Link { get; set; }
        public int? Badge { get; set; }
        public int TotalDevices { get; set; }
        public int DeliveredCount { get; set; }
        public List<TestPushDeviceDto> Devices { get; set; } = new();
    }

    public class TestPushDeviceDto
    {
        public int Platform { get; set; }
        public string TokenPreview { get; set; } = "";
        public DateTime RegisteredAt { get; set; }
        public bool Success { get; set; }
        public string? MessageId { get; set; }
        public string? ErrorCode { get; set; }
        public string? Error { get; set; }
    }
}
