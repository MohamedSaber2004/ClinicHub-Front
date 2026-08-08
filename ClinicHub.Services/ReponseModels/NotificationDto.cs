namespace ClinicHub.Services.ReponseModels
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid? SenderUserId { get; set; }
        public string? TitleEn { get; set; }
        public string? TitleAr { get; set; }
        public string? BodyEn { get; set; }
        public string? BodyAr { get; set; }
        public bool IsRead { get; set; }
        public Guid? ClinicId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Type { get; set; }
    }
}
