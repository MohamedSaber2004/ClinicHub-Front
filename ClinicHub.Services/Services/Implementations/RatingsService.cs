using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.Routes.Api;
using Microsoft.Extensions.Options;

namespace ClinicHub.Services.Services.Implementations
{
    public class RatingsService : IRatingsService
    {
        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        public RatingsService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
        }

        public async Task<List<RatingDto>> GetDoctorRatingsAsync(Guid doctorId)
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.Ratings.DoctorRatings(doctorId));
                return await _deserializerService.DeserializeApiResponse<List<RatingDto>>(response, "حدث خطأ في جلب تقييمات الطبيب");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<List<RatingDto>> GetClinicRatingsAsync(Guid clinicId)
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.Ratings.ClinicRatings(clinicId));
                return await _deserializerService.DeserializeApiResponse<List<RatingDto>>(response, "حدث خطأ في جلب تقييمات العيادة");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<List<RatingDto>> GetPlaceCleanlinessRatingsAsync(Guid clinicId)
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.Ratings.PlaceCleanlinessRatings(clinicId));
                return await _deserializerService.DeserializeApiResponse<List<RatingDto>>(response, "حدث خطأ في جلب تقييمات نظافة المكان");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
