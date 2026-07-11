using ClinicHub.Services.Contracts;
using ClinicHub.Services.Exceptions;
using ClinicHub.Services.ReponseModels;
using ClinicHub.Services.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ClinicHub.Services.Services.Implementations
{
    public class DeserializerService : IDeserializerService
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public async Task<T> DeserializeApiResponse<T>(HttpResponseMessage response, string errorMessage)
        {
            var body = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse<T>>(body, _jsonSettings);

            if (apiResponse != null && apiResponse.Success)
                return apiResponse.Data!;

            var errors = ApiErrorExtractor.ExtractErrors(body);
            var combined = string.Join("\n", errors);

            if (string.IsNullOrWhiteSpace(combined))
                combined = apiResponse?.Message ?? errorMessage;

            var statusCode = apiResponse?.StatusCode > 0 ? apiResponse.StatusCode : (int)response.StatusCode;
            throw new ApiException(statusCode, combined);
        }
    }
}
