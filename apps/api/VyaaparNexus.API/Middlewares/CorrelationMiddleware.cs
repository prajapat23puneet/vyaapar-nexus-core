using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace VyaaparNexus.API.Middlewares;

public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationHeaderName = "X-Correlation-ID";

    public CorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationHeaderName, out StringValues correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers.Append(CorrelationHeaderName, correlationId);
        }

        context.Items["CorrelationId"] = correlationId.ToString();

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationHeaderName))
            {
                context.Response.Headers.Append(CorrelationHeaderName, correlationId);
            }
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
