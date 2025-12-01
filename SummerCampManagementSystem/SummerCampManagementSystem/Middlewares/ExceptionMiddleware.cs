using Microsoft.AspNetCore.Http;
using System.Text.Json;
using SummerCampManagementSystem.BLL.Exceptions;

namespace SummerCampManagementSystem.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BaseException ex) // get exception from BLL
            {
                await WriteErrorResponseAsync(context, ex.StatusCode, ex.Message);
            }
            catch (Exception ex) // get other exceptions
            {
                await WriteErrorResponseAsync(context, 500, "Internal Server Error");
            }
        }

        private async Task WriteErrorResponseAsync(HttpContext context, int statusCode, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var result = JsonSerializer.Serialize(new
            {
                statusCode,
                message
            });

            await context.Response.WriteAsync(result);
        }
    }
}
