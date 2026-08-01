using System.Diagnostics;

namespace SistemaPOS.GVG.API.Middleware
{
    /// <summary>
    /// Middleware para registrar métricas de rendimiento de cada request
    /// </summary>
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

        public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var requestPath = context.Request.Path;
            var requestMethod = context.Request.Method;

            try
            {
                await _next(context);
            }
            finally
            {
                sw.Stop();
                var elapsedMs = sw.ElapsedMilliseconds;
                var statusCode = context.Response.StatusCode;

                if (elapsedMs > 1000) // Más de 1 segundo
                {
                    _logger.LogWarning(
                        "Request lento: {Method} {Path} - {ElapsedMs}ms - Status: {StatusCode}",
                        requestMethod, requestPath, elapsedMs, statusCode);
                }
                else
                {
                    _logger.LogInformation(
                        "{Method} {Path} - {ElapsedMs}ms - Status: {StatusCode}",
                        requestMethod, requestPath, elapsedMs, statusCode);
                }
            }
        }
    }
}
