using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SistemaPOS.API.Models;
using System.Net;

namespace SistemaPOS.GVG.API.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Error no manejado: {Message}", exception.Message);

            var problemDetails = new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                Title = "Ocurrió un error interno en el servidor",
                Detail = _environment.IsDevelopment() ? exception.Message : "Ha ocurrido un error inesperado. Por favor contacte al administrador."
            };

            // Manejo específico para errores de SQL Server
            if (exception is SqlException sqlEx)
            {
                _logger.LogError(sqlEx, "Error de SQL Server. Código de error: {ErrorCode}", sqlEx.Number);

                if (sqlEx.Number == -2) // Timeout
                {
                    problemDetails.Status = (int)HttpStatusCode.GatewayTimeout;
                    problemDetails.Title = "Tiempo de espera de base de datos";
                    problemDetails.Detail = "La conexión a la base de datos tardó demasiado. Intente nuevamente.";
                }
                else if (sqlEx.Number == 4221) // Login failed
                {
                    problemDetails.Status = (int)HttpStatusCode.ServiceUnavailable;
                    problemDetails.Title = "Servicio no disponible";
                    problemDetails.Detail = "No se puede conectar a la base de datos. Verifique las credenciales e intente nuevamente.";
                }
                else
                {
                    problemDetails.Status = (int)HttpStatusCode.ServiceUnavailable;
                    problemDetails.Title = "Error de base de datos";
                    problemDetails.Detail = _environment.IsDevelopment() ? sqlEx.Message : "Error al acceder a la base de datos. Intente nuevamente más tarde.";
                }
            }
            else if (exception is ArgumentException)
            {
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Solicitud inválida";
                problemDetails.Detail = exception.Message;
            }
            else if (exception is UnauthorizedAccessException)
            {
                problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                problemDetails.Title = "No autorizado";
            }
            else if (exception is InvalidOperationException)
            {
                problemDetails.Status = (int)HttpStatusCode.Conflict;
                problemDetails.Title = "Operación no válida";
                problemDetails.Detail = exception.Message;
            }

            httpContext.Response.StatusCode = problemDetails.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}

