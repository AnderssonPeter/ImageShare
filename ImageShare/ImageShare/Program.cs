using ImageShare;
using ImageShare.Authentication;
using ImageShare.Browsing;
using ImageShare.Health;
using ImageShare.Thumbnail;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));
builder.Services.AddOpenIdConnectAuthentication(builder.Configuration);
builder.Services.AddImageShareFilter();
builder.Services.AddUser();
builder.Services.AddThumbnailService();
builder.Services.AddOptions<StorageOptions>().BindConfiguration("Storage").Validated();
builder.Services.AddOptions<ImageFormatOptions>().BindConfiguration("ImageFormats").Validated();
builder.Services.AddSingleton<IFileProvider>(sp => new PhysicalFileProvider(sp.GetRequiredService<IOptions<StorageOptions>>().Value.BasePath));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "ImageShare";
        options.Telemetry = false;
        options.Theme = ScalarTheme.Solarized;
        options.ShowDeveloperTools = DeveloperToolsVisibility.Never;
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
app.MapAuthEndpoints();
app.MapFolderEndpoints();
app.MapImageEndpoints();

await app.RunAsync();
