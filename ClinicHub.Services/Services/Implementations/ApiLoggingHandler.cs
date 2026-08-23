using ClinicHub.Services.Exceptions;
using Microsoft.Extensions.Logging;

namespace ClinicHub.Services.Services.Implementations
{
    /// <summary>
    /// Logs every outgoing backend API request and its response status/duration.
    /// Only the path + query string are logged — the backend host and IP are
    /// never written to any log output, keeping the reverse-proxy transparent.
    /// </summary>
    public class ApiLoggingHandler : DelegatingHandler
    {
        private readonly ILogger<ApiLoggingHandler> _logger;

        public ApiLoggingHandler(ILogger<ApiLoggingHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var method   = request.Method.Method;
            // Log only path + query — never the host or scheme.
            var endpoint = request.RequestUri is { } uri
                ? uri.PathAndQuery
                : "(unknown)";

            _logger.LogInformation(
                "[API ▶] {Method} {Endpoint}",
                method, endpoint);

            var sw = System.Diagnostics.Stopwatch.StartNew();

            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[API ✖] {Method} {Endpoint} — unreachable after {ElapsedMs} ms",
                    method, endpoint, sw.ElapsedMilliseconds);
                throw new ApiException(503,
                    "تعذّر الاتصال بخادم النظام. تأكد من تشغيل الخادم وتوفر الإنترنت ثم أعد المحاولة.");
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[API ✖] {Method} {Endpoint} — timed out after {ElapsedMs} ms",
                    method, endpoint, sw.ElapsedMilliseconds);
                throw new ApiException(504,
                    "استغرق الاتصال بالخادم وقتاً أطول من المعتاد. يرجى المحاولة مرة أخرى.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex,
                    "[API ✖] {Method} {Endpoint} — failed after {ElapsedMs} ms",
                    method, endpoint, sw.ElapsedMilliseconds);
                throw;
            }

            sw.Stop();

            var level = (int)response.StatusCode >= 400
                ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(level,
                "[API ◀] {Method} {Endpoint} → {StatusCode} ({StatusCodeInt}) in {ElapsedMs} ms",
                method, endpoint,
                response.StatusCode,
                (int)response.StatusCode,
                sw.ElapsedMilliseconds);

            return response;
        }
    }
}
