using System.Net;
using Oficina.Cadastro.Application.Shared;

namespace Oficina.Cadastro.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items[CorrelationIdMiddleware.HeaderName]?.ToString()
                ?? context.TraceIdentifier;

            if (ex is OficinaException oficinaException)
            {
                logger.LogWarning(ex, "Public application error. CorrelationId: {CorrelationId}", correlationId);
                context.Response.StatusCode = oficinaException.StatusHttp;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new
                {
                    title = oficinaException.Message,
                    status = oficinaException.StatusHttp,
                    correlationId
                });
                return;
            }

            logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/500",
                title = "Internal Server Error",
                status = StatusCodes.Status500InternalServerError,
                correlationId
            });
        }
    }
}
