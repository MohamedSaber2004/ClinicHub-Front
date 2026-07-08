using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ClinicHub.Services.Utilities
{
    public static class ApiErrorExtractor
    {
        public static List<string> ExtractErrors(string body)
        {
            var errors = new List<string>();

            try
            {
                var obj = JsonConvert.DeserializeObject<JObject>(body);
                if (obj == null) return errors;

                var message = obj["message"]?.ToString() ?? obj["Message"]?.ToString() ?? "";

                var errorsToken = obj["errors"] ?? obj["Errors"];
                if (errorsToken != null && errorsToken.Type == JTokenType.Object)
                {
                    foreach (var prop in errorsToken.Children<JProperty>())
                    {
                        if (prop.Value.Type == JTokenType.Array)
                        {
                            foreach (var item in prop.Value)
                            {
                                var msg = item.ToString();
                                if (!string.IsNullOrWhiteSpace(msg))
                                    errors.Add(msg);
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(message))
                {
                    if (errors.Count == 0)
                        errors.Add(message);
                    else
                        errors.Insert(0, message);
                }
            }
            catch
            {
                // fallback — keep errors empty
            }

            return errors;
        }
    }
}
