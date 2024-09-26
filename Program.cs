using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MySmartHomeAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton<IDeviceService, DeviceService>();

var app = builder.Build();
app.UseWebSockets();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseRouting();

// Register ApiKeyMiddleware
app.UseMiddleware<ApiKeyMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();
