using System.Text;
using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;
using ClinicHub.Services.Routes.Api;
using ClinicHub.Services.Utilities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ClinicHub.Services.Services.Implementations
{
    public class AdService : IAdService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        public AdService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
        }

        public async Task<List<AdDto>> GetMyAdsAsync(Guid clinicId, int? status = null)
        {
            try
            {
                var url = DoctoryRoutes.Ads.MyAds(clinicId);
                if (status.HasValue) url += $"?Status={status.Value}";

                var response = await _httpClient.GetAsync(url);
                return await _deserializerService.DeserializeApiResponse<List<AdDto>>(response, "حدث خطأ في جلب إعلاناتك");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<List<AdPackageDto>> GetPackagesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.Ads.Packages);
                return await _deserializerService.DeserializeApiResponse<List<AdPackageDto>>(response, "حدث خطأ في جلب باقات الإعلانات");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<AdsOrderResponseDto> CreateOrderAsync(Guid clinicId, CreateAdsOrderRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(DoctoryRoutes.Ads.CreateOrder(clinicId), content);
                return await _deserializerService.DeserializeApiResponse<AdsOrderResponseDto>(response, "حدث خطأ في إنشاء طلب الإعلان");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<PagginatedResult<AdDto>> GetAdsAsync(int pageNumber = 1, int pageSize = 20, int? status = null)
        {
            try
            {
                var url = $"{DoctoryRoutes.AdminAds.List}?PageNumber={pageNumber}&PageSize={pageSize}";
                if (status.HasValue) url += $"&Status={status.Value}";

                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                var obj = JsonConvert.DeserializeObject<JObject>(body);
                var dataToken = obj?["data"] ?? obj?["Data"];
                var dataJson = dataToken?.ToString() ?? body;
                return JsonConvert.DeserializeObject<PagginatedResult<AdDto>>(dataJson, _jsonSettings)
                    ?? new PagginatedResult<AdDto>(new List<AdDto>(), 0);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<bool> DeactivateAdAsync(Guid id, string? reason = null)
        {
            try
            {
                var json = JsonConvert.SerializeObject(new { reason }, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(DoctoryRoutes.AdminAds.Deactivate(id), content);
                return await _deserializerService.DeserializeApiResponse<bool>(response, "حدث خطأ في إلغاء الإعلان");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<List<AdPackageDto>> GetAllPackagesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.AdminAds.Packages);
                return await _deserializerService.DeserializeApiResponse<List<AdPackageDto>>(response, "حدث خطأ في جلب باقات الإعلانات");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<AdPackageDto> CreatePackageAsync(UpsertAdPackageRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(DoctoryRoutes.AdminAds.Packages, content);
                return await _deserializerService.DeserializeApiResponse<AdPackageDto>(response, "حدث خطأ في إضافة الباقة");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<AdPackageDto> UpdatePackageAsync(Guid id, UpsertAdPackageRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request, _jsonSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(DoctoryRoutes.AdminAds.Package(id), content);
                return await _deserializerService.DeserializeApiResponse<AdPackageDto>(response, "حدث خطأ في تعديل الباقة");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<bool> DeletePackageAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(DoctoryRoutes.AdminAds.Package(id));
                return await _deserializerService.DeserializeApiResponse<bool>(response, "حدث خطأ في حذف الباقة");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
