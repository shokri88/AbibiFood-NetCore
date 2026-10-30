using System.Security.Claims;
using AbibiFood.Domain.Entities;
using AbibiFood.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace AbibiFood.Api.Middleware;

public sealed class ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopes)
    {
        try { await next(context); }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var status = ex switch
            {
                UnauthorizedAccessException => 403,
                ArgumentException => 400,
                AbibiFood.Domain.Rules.ConflictException => 409,
                DbUpdateConcurrencyException => 409,
                _ => 500
            };
            if (status == 500)
            {
                try
                {
                    // Context مجزا: تغییرات ناموفق درخواست وارد ذخیره لاگ نمی‌شوند.
                    using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.ErrorLogs.Add(new ErrorLog
                    {
                        CorrelationId = context.TraceIdentifier,
                        ActorId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "",
                        RequestPath = context.Request.Path,
                        ExceptionType = ex.GetType().Name,
                        Message = "Unhandled server error",
                        StackTrace = ex.StackTrace ?? ""
                    });
                    await db.SaveChangesAsync();
                }
                catch (Exception logError) { logger.LogError(logError, "SQL error log unavailable. Correlation {Id}", context.TraceIdentifier); }
            }
            context.Response.StatusCode = status;
            if (context.Request.Path.StartsWithSegments("/api"))
                await Results.Problem(statusCode: status, title: status == 500 ? "خطای داخلی؛ شناسه پیگیری را به پشتیبانی بدهید." : ex.Message,
                    extensions: new Dictionary<string, object?> { ["correlationId"] = context.TraceIdentifier }).ExecuteAsync(context);
            else { context.Response.ContentType = "text/plain; charset=utf-8"; await context.Response.WriteAsync(status == 500 ? "خطای داخلی. شناسه پیگیری: " + context.TraceIdentifier : ex.Message); }
        }
    }
}
