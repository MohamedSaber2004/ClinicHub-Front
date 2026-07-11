namespace ClinicHub.Services.Contracts
{
    public interface IDeserializerService
    {
        Task<T> DeserializeApiResponse<T>(HttpResponseMessage response, string errorMessage);
    }
}
