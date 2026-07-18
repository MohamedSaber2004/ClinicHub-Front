using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;
using ClinicHub.Services.Routes.Api;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ClinicHub.Services.Services.Implementations
{
    public class ClinicService : IClinicService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        private readonly string _getAllClinicsForViewingOnly;

        public ClinicService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);

            _getAllClinicsForViewingOnly = DoctoryRoutes.Doctors.GetAllClinicsForViewingOnly;
        }

        public async Task<ApiResponse<List<ClinicLookupDto>>> GetAllClinicsForViewingOnlyAsync(GetAllCLinicsForViewingOnly request)
        {
            try
            {
                var response = await _httpClient.GetAsync(_getAllClinicsForViewingOnly);
                var data = await _deserializerService.DeserializeApiResponse<List<ClinicLookupDto>>(response, "حدث خطأ في جلب العيادات");

                return new ApiResponse<List<ClinicLookupDto>>
                {
                    Success = true,
                    Data = data
                };
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
