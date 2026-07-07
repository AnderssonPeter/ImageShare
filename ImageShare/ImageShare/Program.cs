using ImageShare;
using ImageShare.Authentication;
using ImageShare.Browsing;
using ImageShare.Health;
using ImageShare.ImageConversion;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Mirality.FileProviders;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));
builder.Services.AddOpenIdConnectAuthentication(builder.Configuration);
builder.Services.AddImageShareFilter();
builder.Services.AddUser();
builder.Services.AddImageConversion();
builder.Services.AddOptions<StorageOptions>().BindConfiguration("Storage").Validated();
builder.Services.AddOptions<ImageFormatOptions>().BindConfiguration("ImageFormats").Validated();
builder.Services.AddSingleton<IContentTypeProvider>(serviceProvider =>
{
    var provider = new FileExtensionContentTypeProvider();
    provider.Mappings[".avif"] = "image/avif";
    return provider;
});
builder.Services.AddSingleton<IWritableFileProvider>(serviceProvider =>
{
    var basePath = serviceProvider.GetRequiredService<IOptions<StorageOptions>>().Value.BasePath;
    return new WritablePhysicalFileProvider(basePath, new PhysicalFileProvider(basePath));
});
builder.Services.AddSingleton<IFileProvider>(serviceProvider =>
    serviceProvider.GetRequiredService<IWritableFileProvider>());

var app = builder.Build();

var storageBasePath = app.Services.GetRequiredService<IOptions<StorageOptions>>().Value.BasePath;
#pragma warning disable RS0030
var baseDirectory = Path.GetFullPath(storageBasePath);
if (!Directory.Exists(baseDirectory))
{
    Directory.CreateDirectory(baseDirectory);
}
#pragma warning restore RS0030

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
