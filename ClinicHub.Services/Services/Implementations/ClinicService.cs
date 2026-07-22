using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.Options;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.RequestModels;
using ClinicHub.Services.Routes.Api;
using ClinicHub.Services.Utilities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ClinicHub.Services.Services.Implementations
{
    public class ClinicService : IClinicService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter> { new StringEnumConverter() }
        };

        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        private readonly string _getAllClinicsForViewingOnly;
        private readonly string _getAllClinicsPaginated;
        private readonly Func<Guid, string> _getClinicById;
        private readonly string _createClinic;
        private readonly Func<Guid, string> _updateClinic;

        public ClinicService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);

            _getAllClinicsForViewingOnly = DoctoryRoutes.Doctors.GetAllClinicsForViewingOnly;
            _getAllClinicsPaginated = DoctoryRoutes.Clinics.GetAll;
            _getClinicById = DoctoryRoutes.Clinics.GetById;
            _createClinic = DoctoryRoutes.Clinics.Create;
            _updateClinic = DoctoryRoutes.Clinics.Update;
        }

        public async Task<ApiResponse<ClinicManagmentDto>> CreateClinicAsync(CreateClinicRequest request)
        {
            try
            {
                if (request == null)
                    throw new ApiException(400, "بيانات العيادة مطلوبة");

                var payload = new JObject();
                AddIfNotNull(payload, "nameAr", request.NameAr);
                AddIfNotNull(payload, "arDescription", request.ArDescription);
                AddIfNotNull(payload, "addressAr", request.AddressAr);
                AddIfNotNull(payload, "phone", request.Phone);
                AddIfNotNull(payload, "email", request.Email);
                AddIfNotNull(payload, "website", request.Website);
                AddIfNotNull(payload, "logo", request.Logo);
                AddIfNotNull(payload, "specializationId", request.SpecializationId.ToString());
                AddIfNotNull(payload, "ownerName", request.OwnerName);
                AddIfNotNull(payload, "ownerEmail", request.OwnerEmail);
                AddIfNotNull(payload, "ownerPhone", request.OwnerPhone);
                if (request.Lat.HasValue) payload["lat"] = request.Lat.Value;
                if (request.Lng.HasValue) payload["lng"] = request.Lng.Value;
                AddIfNotNull(payload, "workingHoursStart", request.WorkingHoursStart);
                AddIfNotNull(payload, "workingHoursEnd", request.WorkingHoursEnd);
                if (request.WorkingDays != null && request.WorkingDays.Count > 0)
                    payload["workingDays"] = new JArray(request.WorkingDays.Select(d => (int)d).ToList());
                if (request.DoctorSpecializationId != Guid.Empty)
                    payload["doctorSpecializationId"] = request.DoctorSpecializationId.ToString();
                AddIfNotNull(payload, "bio", request.Bio);
                if (request.YearsOfExperience > 0)
                    payload["yearsOfExperience"] = request.YearsOfExperience;

                var json = payload.ToString(Formatting.None);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_createClinic, content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errs = ApiErrorExtractor.ExtractErrors(body);
                    var combined = string.Join("<br />", errs);
                    if (string.IsNullOrWhiteSpace(combined))
                        combined = "حدث خطأ في إنشاء العيادة";
                    throw new ApiException((int)response.StatusCode, combined);
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ClinicManagmentDto>>(body, _jsonSettings);
                if (apiResponse?.Data != null && apiResponse.Data.Id != Guid.Empty)
                    return new ApiResponse<ClinicManagmentDto>
                    {
                        Success = true,
                        Data = apiResponse.Data,
                        Message = apiResponse.Message
                    };

                throw new ApiException(500, "حدث خطأ في إنشاء العيادة");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        private static void AddIfNotNull(JObject obj, string key, string? value)
        {
            if (!string.IsNullOrEmpty(value))
                obj[key] = value;
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

        public async Task<PagginatedResult<ClinicManagmentDto>> GetAllClinicsPaginatedAsync(GetAllClinicsPagginatedRequest request)
        {
            try
            {
                var url = $"{_getAllClinicsPaginated}?PageNumber={request.PageNumber}&PageSize={request.PageSize}";

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                    url += $"&SearchTerm={Uri.EscapeDataString(request.SearchTerm)}";
                if (request.Status.HasValue)
                    url += $"&Status={(int)request.Status.Value}";
                if (!string.IsNullOrWhiteSpace(request.Name))
                    url += $"&Name={Uri.EscapeDataString(request.Name)}";
                if (!string.IsNullOrWhiteSpace(request.Email))
                    url += $"&Email={Uri.EscapeDataString(request.Email)}";
                if (!string.IsNullOrWhiteSpace(request.Phone))
                    url += $"&Phone={Uri.EscapeDataString(request.Phone)}";
                if (request.CreatedFrom.HasValue)
                    url += $"&CreatedFrom={request.CreatedFrom.Value:yyyy-MM-dd}";
                if (request.CreatedTo.HasValue)
                    url += $"&CreatedTo={request.CreatedTo.Value:yyyy-MM-dd}";
                if (!string.IsNullOrWhiteSpace(request.SortBy))
                    url += $"&SortBy={Uri.EscapeDataString(request.SortBy)}";
                url += $"&SortAscending={request.SortAscending.ToString().ToLower()}";

                var response = await _httpClient.GetAsync(url);
                return await _deserializerService.DeserializeApiResponse<PagginatedResult<ClinicManagmentDto>>(response, "حدث خطأ في جلب العيادات");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ClinicManagmentDto>> GetClinicByIdAsync(GetClinicByIdRequest request)
        {
            try
            {
                var url = _getClinicById(request.Id);
                var response = await _httpClient.GetAsync(url);
                var data = await _deserializerService.DeserializeApiResponse<ClinicManagmentDto>(response, "حدث خطأ في جلب بيانات العيادة");

                return new ApiResponse<ClinicManagmentDto>
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

        public async Task<ApiResponse<ClinicManagmentDto>> UpdateClinicAsync(UpdateClinicRequest request)
        {
            try
            {
                if (request == null)
                    throw new ApiException(400, "بيانات العيادة مطلوبة");

                var url = _updateClinic(request.Id);
                var payload = new JObject();
                AddIfNotNull(payload, "name", request.Name);
                AddIfNotNull(payload, "nameAr", request.NameAr);
                AddIfNotNull(payload, "description", request.Description);
                AddIfNotNull(payload, "arDescription", request.ArDescription);
                AddIfNotNull(payload, "address", request.Address);
                AddIfNotNull(payload, "addressAr", request.AddressAr);
                AddIfNotNull(payload, "phone", request.Phone);
                AddIfNotNull(payload, "email", request.Email);
                AddIfNotNull(payload, "website", request.Website);
                AddIfNotNull(payload, "logo", request.Logo);
                AddIfNotNull(payload, "specializationId", request.SpecializationId.ToString());
                AddIfNotNull(payload, "workingHours", request.WorkingHours);
                if (request.WorkingHoursStart.HasValue) payload["workingHoursStart"] = request.WorkingHoursStart.Value.ToString("HH:mm:ss");
                if (request.WorkingHoursEnd.HasValue) payload["workingHoursEnd"] = request.WorkingHoursEnd.Value.ToString("HH:mm:ss");
                if (request.WorkingDays != null && request.WorkingDays.Count > 0)
                    payload["workingDays"] = new JArray(request.WorkingDays.Select(d => (int)d).ToList());

                var json = payload.ToString(Formatting.None);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errs = ApiErrorExtractor.ExtractErrors(body);
                    var combined = string.Join("<br />", errs);
                    if (string.IsNullOrWhiteSpace(combined))
                        combined = "حدث خطأ في تحديث العيادة";
                    throw new ApiException((int)response.StatusCode, combined);
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<ClinicManagmentDto>>(body, _jsonSettings);
                if (apiResponse?.Data != null)
                    return new ApiResponse<ClinicManagmentDto>
                    {
                        Success = true,
                        Data = apiResponse.Data,
                        Message = apiResponse.Message ?? "تم تحديث العيادة بنجاح"
                    };

                throw new ApiException(500, "حدث خطأ في تحديث العيادة");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
