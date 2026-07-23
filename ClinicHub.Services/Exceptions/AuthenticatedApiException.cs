using ClinicHub.Services.ReponseModels;

namespace ClinicHub.Services.Exceptions
{
    public class AuthenticatedApiException : ApiException
    {
        public AuthResponseDto AuthData { get; }

        public AuthenticatedApiException(int statusCode, string message, AuthResponseDto authData)
            : base(statusCode, message)
        {
            AuthData = authData;
        }
    }
}
