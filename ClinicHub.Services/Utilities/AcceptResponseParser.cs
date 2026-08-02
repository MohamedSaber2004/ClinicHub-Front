using ClinicHub.Services.ReponseModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ClinicHub.Services.Utilities
{
    /// <summary>
    /// Parses accept/approve command responses (appointment request → payment flow).
    /// New contract returns a payment envelope object (docs/appointment-request-payment-flow.md §3.3);
    /// legacy backends return <c>data: true</c> — both shapes are handled.
    /// </summary>
    public static class AcceptResponseParser
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public static AppointmentAcceptResponseDto? Parse(string body)
        {
            var json = JsonConvert.DeserializeObject<JObject>(body);
            var dataToken = json?["data"] ?? json?["Data"];

            if (dataToken == null || dataToken.Type == JTokenType.Null || dataToken.Type == JTokenType.Boolean)
                return new AppointmentAcceptResponseDto();

            var serializer = JsonSerializer.Create(_jsonSettings);
            return dataToken.ToObject<AppointmentAcceptResponseDto>(serializer) ?? new AppointmentAcceptResponseDto();
        }
    }
}
