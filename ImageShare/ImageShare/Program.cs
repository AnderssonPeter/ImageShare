using ImageShare.Endpoints;
using ImageShare.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddOpenIdConnectAuthentication(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapWeatherForecastEndpoints();
app.MapAuthEndpoints();

app.Run();
