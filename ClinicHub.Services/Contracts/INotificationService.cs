using ClinicHub.Services.ReponseModels;

namespace ClinicHub.Services.Contracts
{
    public interface INotificationService
    {
        Task<int> GetUnreadCountAsync();
        Task<PagginatedResult<NotificationDto>> GetNotificationsAsync(int pageNumber, int pageSize);
    }
}
