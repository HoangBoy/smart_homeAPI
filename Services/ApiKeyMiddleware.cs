using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "Authorization"; // Tên header chứa API Key
    private readonly string _apiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _apiKey = config.GetValue<string>("ApiKey"); // Lấy API Key từ cấu hình
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Response.StatusCode = 401; // Unauthorized
            await context.Response.WriteAsync("API Key không được cung cấp.");
            return;
        }

        // Xóa "Bearer " nếu nó có mặt
        var apiKey = extractedApiKey.ToString().StartsWith("Bearer ") 
            ? extractedApiKey.ToString().Substring("Bearer ".Length).Trim() 
            : extractedApiKey.ToString();

        if (!_apiKey.Equals(apiKey))
        {
            context.Response.StatusCode = 401; // Unauthorized
            await context.Response.WriteAsync("API Key không hợp lệ.");
            return;
        }

        await _next(context);
    }
}
