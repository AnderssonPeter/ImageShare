using ImageShare;
using ImageShare.Authentication;
using ImageShare.Health;
using ImageShare.Thumbnail;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));
builder.Services.AddOpenIdConnectAuthentication(builder.Configuration);
builder.Services.AddImageShareFilter();
builder.Services.AddUser();
builder.Services.AddThumbnailService(builder.Configuration);
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "ImageShare";
        options.Telemetry = false;
        options.Theme = ScalarTheme.Solarized;
        options.Agent = new ScalarAgentOptions
        {
            Disabled = true
        };
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapWeatherForecastEndpoints();
app.MapAuthEndpoints();
app.MapFolderEndpoints();

await app.RunAsync();
