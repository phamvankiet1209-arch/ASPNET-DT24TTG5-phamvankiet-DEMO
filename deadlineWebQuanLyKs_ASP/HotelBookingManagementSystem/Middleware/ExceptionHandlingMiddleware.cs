using System.Net;
using HotelBookingManagementSystem.Models;

namespace HotelBookingManagementSystem.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);

                if (context.Response.HasStarted) return;
                if (context.Request.Path.StartsWithSegments("/Home/Error")) return;

                if (context.Response.StatusCode == (int)HttpStatusCode.NotFound)
                    context.Response.Redirect("/Home/Error?code=404");
                else if (context.Response.StatusCode == (int)HttpStatusCode.Forbidden)
                    context.Response.Redirect("/Home/Error?code=403");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                if (!context.Response.HasStarted)
                    context.Response.Redirect("/Home/Error?code=500");
            }
        }
    }
}
