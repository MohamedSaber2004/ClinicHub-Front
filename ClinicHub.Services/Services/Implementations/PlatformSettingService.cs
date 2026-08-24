using System.Text;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.Routes.Api;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClinicHub.Services.Services.Implementations
{
    public class PlatformSettingService : IPlatformSettingService
    {
        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        public PlatformSettingService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
        }

        public async Task<PlatformSettingDto> GetSettingAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.PlatformSettings.Get);
                return await _deserializerService.DeserializeApiResponse<PlatformSettingDto>(response, "تعذر تحميل إعدادات رسوم المنصة")
                    ?? new PlatformSettingDto();
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"تعذر تحميل إعدادات رسوم المنصة: {ex.Message}");
            }
        }

        public async Task<PlatformSettingDto> UpdateSettingAsync(decimal appointmentFeePercent)
        {
            try
            {
                var payload = new { appointmentFeePercent };
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(DoctoryRoutes.PlatformSettings.Update, content);
                return await _deserializerService.DeserializeApiResponse<PlatformSettingDto>(response, "تعذر تحديث نسبة رسوم المنصة")
                    ?? new PlatformSettingDto { AppointmentFeePercent = appointmentFeePercent };
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"تعذر تحديث نسبة رسوم المنصة: {ex.Message}");
            }
        }
    }
}
