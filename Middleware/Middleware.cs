using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using RozitekAPIConnector.Models;
using System.Threading.Tasks;

namespace RozitekAPIConnector.Middleware
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class Middleware
    {
        private readonly RequestDelegate _next;
        private readonly AppSettings _appSettings;

        public Middleware(RequestDelegate next, IOptions<AppSettings> appConfig)
        {
            _next = next;
            _appSettings = appConfig.Value;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                string token = httpContext.Request.Headers["token"];
                if (token == null || token == "")
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await httpContext.Response.WriteAsJsonAsync(new
                    {
                        Id = -1,
                        Message = "Token is null"
                    });
                }

                if (token != _appSettings.Token)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await httpContext.Response.WriteAsJsonAsync(new
                    {
                        Id = -1,
                        Message = "Token is not validate"
                    });
                }

                await _next(httpContext);
            }
            catch(Exception ex)
            {
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    Id = -1,
                    Message = ex.Message
                });
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<Middleware>();
        }
    }
}
