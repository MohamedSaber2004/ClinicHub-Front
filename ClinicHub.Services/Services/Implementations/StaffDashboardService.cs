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
    public class StaffDashboardService : IStaffDashboardService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        private static readonly Dictionary<string, (string label, string cssClass)> StatusMap = new()
        {
            ["pending"] = ("قيد الانتظار", "badge-warning"),
            ["confirmed"] = ("مؤكد", "badge-success"),
            ["cancelled"] = ("ملغي", "badge-danger"),
            ["completed"] = ("منتهي", "badge-info"),
            ["in-progress"] = ("قيد الكشف", "badge-primary"),
            ["waiting"] = ("في الانتظار", "badge-warning"),
            ["registered"] = ("تم التسجيل", "badge-info"),
            ["accepted"] = ("بانتظار الدفع", "badge-warning"),
            ["awaiting-payment"] = ("بانتظار الدفع", "badge-warning"),
            ["rejected"] = ("مرفوض", "badge-danger"),
            ["noshow"] = ("لم يحضر", "badge-danger")
        };

        private static readonly Dictionary<int, string> StatusIntMap = new()
        {
            [0] = "pending",
            [1] = "confirmed",
            [2] = "cancelled",
            [3] = "completed",
            [4] = "reserved",
            [5] = "noshow",
            [6] = "accepted",
            [7] = "rejected"
        };

        private static string NormalizeStatus(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw ?? "";
            if (int.TryParse(raw, out var num) && StatusIntMap.ContainsKey(num))
                return StatusIntMap[num];
            return raw.ToLowerInvariant();
        }

        private static void EnrichStatus<T>(T item) where T : class
        {
            var statusProp = item.GetType().GetProperty("Status");
            var labelProp = item.GetType().GetProperty("StatusLabel");
            var classProp = item.GetType().GetProperty("StatusClass");
            if (statusProp == null) return;

            var rawStatus = statusProp.GetValue(item) as string;
            if (string.IsNullOrWhiteSpace(rawStatus)) return;

            var status = NormalizeStatus(rawStatus);

            if (!string.Equals(rawStatus, status, StringComparison.OrdinalIgnoreCase))
                statusProp.SetValue(item, status);

            if (labelProp != null && string.IsNullOrWhiteSpace(labelProp.GetValue(item) as string)
                && StatusMap.TryGetValue(status, out var entry))
            {
                labelProp.SetValue(item, entry.label);
                if (classProp != null && string.IsNullOrWhiteSpace(classProp.GetValue(item) as string))
                    classProp.SetValue(item, entry.cssClass);
            }
        }

        private readonly HttpClient _httpClient;
        private readonly IDeserializerService _deserializerService;

        public StaffDashboardService(HttpClient httpClient, IOptions<Doctory> doctoryOptions, IDeserializerService deserializerService)
        {
            _httpClient = httpClient;
            _deserializerService = deserializerService;
            DoctoryRoutes.Initialize(doctoryOptions.Value.BaseUrl);
        }

        public async Task<StaffDashboardStatsDto> GetStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.StaffDashboard.Stats);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                var json = JsonConvert.DeserializeObject<JObject>(body);
                if (json == null)
                    throw new ApiException(500, "استجابة فارغة من الخادم");

                var successToken = json["success"] ?? json["Success"];
                if (successToken != null && successToken.Type == JTokenType.Boolean && !(bool)successToken)
                {
                    var msg = json["message"]?.ToString() ?? json["Message"]?.ToString() ?? "فشل تحميل الإحصائيات";
                    throw new ApiException(400, msg);
                }

                var dataToken = json["data"] ?? json["Data"];
                if (dataToken != null && dataToken.Type == JTokenType.Object)
                {
                    var serializer = JsonSerializer.Create(_jsonSettings);
                    return dataToken.ToObject<StaffDashboardStatsDto>(serializer) ?? new StaffDashboardStatsDto();
                }

                return new StaffDashboardStatsDto();
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<PagginatedResult<StaffAppointmentDto>> GetAppointmentsAsync(string? status = null, string? date = null, string? patientName = null, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var url = $"{DoctoryRoutes.StaffDashboard.Appointments}?pageNumber={pageNumber}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";
                if (!string.IsNullOrWhiteSpace(date)) url += $"&date={Uri.EscapeDataString(date)}";
                if (!string.IsNullOrWhiteSpace(patientName)) url += $"&patientName={Uri.EscapeDataString(patientName)}";

                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                var json = JsonConvert.DeserializeObject<JObject>(body);
                if (json == null)
                    throw new ApiException(500, "استجابة فارغة من الخادم");

                var successToken = json["success"] ?? json["Success"];
                if (successToken != null && successToken.Type == JTokenType.Boolean && !(bool)successToken)
                {
                    var msg = json["message"]?.ToString() ?? json["Message"]?.ToString() ?? "فشل تحميل المواعيد";
                    throw new ApiException(400, msg);
                }

                var serializer = JsonSerializer.Create(_jsonSettings);
                var dataToken = json["data"] ?? json["Data"];

                if (dataToken != null && dataToken.Type == JTokenType.Object)
                {
                    var itemsToken = dataToken["items"] ?? dataToken["Items"];
                    List<StaffAppointmentDto>? items = null;

                    if (itemsToken != null && itemsToken.Type == JTokenType.Array)
                        items = itemsToken.ToObject<List<StaffAppointmentDto>>(serializer);
                    else if (itemsToken is JObject itemsObj)
                        items = itemsObj.Properties()
                            .Select(p => p.Value.ToObject<StaffAppointmentDto>(serializer))
                            .Where(x => x != null)
                            .Cast<StaffAppointmentDto>()
                            .ToList();

                    if (items != null)
                    {
                        int page = dataToken["pageNumber"]?.Value<int>() ?? dataToken["PageNumber"]?.Value<int>() ?? pageNumber;
                        int size = dataToken["pageSize"]?.Value<int>() ?? dataToken["PageSize"]?.Value<int>() ?? pageSize;
                        int count = dataToken["totalCount"]?.Value<int>() ?? dataToken["TotalCount"]?.Value<int>() ?? items.Count;

                        foreach (var item in items) EnrichStatus(item);
                        return PagginatedResult<StaffAppointmentDto>.Create(items.AsReadOnly(), count, page, size);
                    }
                }

                throw new ApiException(500, $"استجابة غير متوقعة من الخادم: {(body.Length > 200 ? body[..200] + "..." : body)}");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<List<StaffQueueItemDto>> GetQueueAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.StaffDashboard.Queue);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                var json = JsonConvert.DeserializeObject<JObject>(body);
                if (json == null)
                    throw new ApiException(500, $"استجابة فارغة من الخادم");

                var serializer = JsonSerializer.Create(_jsonSettings);
                List<StaffQueueItemDto>? result = null;
                var dataToken = json["data"] ?? json["Data"];
                if (dataToken != null && dataToken.Type == JTokenType.Object)
                {
                    var itemsToken = dataToken["items"] ?? dataToken["Items"];
                    if (itemsToken != null && itemsToken.Type == JTokenType.Array)
                        result = itemsToken.ToObject<List<StaffQueueItemDto>>(serializer);
                }

                var successToken = json["success"] ?? json["Success"];
                if (result == null && successToken != null && successToken.Type == JTokenType.Boolean && !(bool)successToken)
                {
                    var msg = json["message"]?.ToString() ?? json["Message"]?.ToString() ?? "فشل تحميل الطابور";
                    throw new ApiException(400, msg);
                }

                if (result == null && dataToken != null && dataToken.Type == JTokenType.Array)
                    result = dataToken.ToObject<List<StaffQueueItemDto>>(serializer);

                if (result == null)
                {
                    var topItems = json["items"] ?? json["Items"];
                    if (topItems != null && topItems.Type == JTokenType.Array)
                        result = topItems.ToObject<List<StaffQueueItemDto>>(serializer);
                }

                if (result != null) { foreach (var item in result) EnrichStatus(item); return result; }

                throw new ApiException(500, $"استجابة غير متوقعة من الخادم: {(body.Length > 200 ? body[..200] + "..." : body)}");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<AppointmentAcceptResponseDto?> ApproveAppointmentAsync(string id)
        {
            try
            {
                var response = await _httpClient.PutAsync(DoctoryRoutes.StaffDashboard.Approve(id), null);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                return AcceptResponseParser.Parse(body);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<bool> RejectAppointmentAsync(string id, string? reason = null)
        {
            try
            {
                var payload = reason != null ? new { reason } : null;
                var json = payload != null ? JsonConvert.SerializeObject(payload) : "";
                var content = payload != null ? new StringContent(json, Encoding.UTF8, "application/json") : null;
                var response = await _httpClient.PutAsync(DoctoryRoutes.StaffDashboard.Reject(id), content);
                return await _deserializerService.DeserializeApiResponse<bool>(response, "حدث خطأ في رفض الموعد");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<bool> CheckInPatientAsync(string id)
        {
            try
            {
                var response = await _httpClient.PutAsync(DoctoryRoutes.StaffDashboard.CheckIn(id), null);
                return await _deserializerService.DeserializeApiResponse<bool>(response, "حدث خطأ في تسجيل الوصول");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<bool> CompleteAppointmentAsync(string id)
        {
            try
            {
                var response = await _httpClient.PutAsync(DoctoryRoutes.StaffDashboard.Complete(id), null);
                return await _deserializerService.DeserializeApiResponse<bool>(response, "حدث خطأ في إنهاء الموعد");
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<RegisterPatientResultDto> RegisterPatientAsync(RegisterPatientFromStaffRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(DoctoryRoutes.StaffDashboard.RegisterPatient, content);
                var body = await response.Content.ReadAsStringAsync();

                var errors = ApiErrorExtractor.ExtractErrors(body);
                if (!response.IsSuccessStatusCode)
                {
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<RegisterPatientResultDto>>(body, _jsonSettings);
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                    return apiResponse.Data;

                var direct = JsonConvert.DeserializeObject<RegisterPatientResultDto>(body, _jsonSettings);
                if (direct != null) return direct;

                var obj = JsonConvert.DeserializeObject<JObject>(body);
                var dataToken = obj?["data"] ?? obj?["Data"];
                if (dataToken != null)
                {
                    var fromData = dataToken.ToObject<RegisterPatientResultDto>();
                    if (fromData != null) return fromData;
                }

                var combined = string.Join(" ", errors);
                throw new ApiException(400, string.IsNullOrWhiteSpace(combined) ? "فشل تسجيل المريض" : combined);
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<List<StaffDoctorDto>> GetDoctorsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(DoctoryRoutes.StaffDashboard.Doctors);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<List<StaffDoctorDto>>>(body, _jsonSettings);
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                    return apiResponse.Data;

                return JsonConvert.DeserializeObject<List<StaffDoctorDto>>(body, _jsonSettings) ?? new List<StaffDoctorDto>();
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }

        public async Task<StaffDoctorScheduleDto> GetDoctorScheduleAsync(string doctorId, string? date = null)
        {
            try
            {
                var url = DoctoryRoutes.StaffDashboard.DoctorSchedule(doctorId);
                if (!string.IsNullOrWhiteSpace(date))
                    url += $"?date={Uri.EscapeDataString(date)}";

                var response = await _httpClient.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errors = ApiErrorExtractor.ExtractErrors(body);
                    throw new ApiException((int)response.StatusCode, string.Join(" ", errors));
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse<StaffDoctorScheduleDto>>(body, _jsonSettings);
                return apiResponse?.Data ?? new StaffDoctorScheduleDto();
            }
            catch (ApiException) { throw; }
            catch (Exception ex)
            {
                throw new ApiException(500, $"حدث خطأ غير متوقع: {ex.Message}");
            }
        }
    }
}
