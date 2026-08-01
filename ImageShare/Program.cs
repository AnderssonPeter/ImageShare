using ImageShare;
using ImageShare.Authentication;
using ImageShare.Browsing;
using ImageShare.Errors;
using ImageShare.Health;
using ImageShare.ImageConversion;
using ImageShare.Spa;
using Mediator;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Mirality.FileProviders;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddImageShareOpenApi();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));
builder.Services.AddCustomErrors();
builder.Services.AddAuthentications();
builder.Services.AddImageShareFilter();
builder.Services.AddUser();
builder.Services.AddJwtTokens();
builder.Services.AddRateLimiting();
builder.Services.AddImageConversion();
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthenticationBehavior<,>));
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AdminBehavior<,>));
builder.Services.AddSingleton<ImageShare.Browsing.ImageEnumerator>();
builder.Services.AddOptions<StorageOptions>().BindConfiguration("Storage").Validated();
builder.Services.AddOptions<ImageFormatOptions>().BindConfiguration("ImageFormats").Validated();
builder.Services.AddSpaHosting();
builder.Services.AddSingleton<IContentTypeProvider>(serviceProvider =>
{
    var provider = new FileExtensionContentTypeProvider();
    provider.Mappings[".avif"] = "image/avif";
    return provider;
});
builder.Services.AddSingleton<IWritableFileProvider>(serviceProvider =>
{
    var basePath = serviceProvider.GetRequiredService<IOptions<StorageOptions>>().Value.BasePath;
#pragma warning disable RS0030
    var fullPath = Path.GetFullPath(basePath);
#pragma warning restore RS0030
    return new WritablePhysicalFileProvider(basePath, new PhysicalFileProvider(fullPath));
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

var api = app.MapGroup("/api");

if (app.Environment.IsDevelopment())
{
    api.MapOpenApi();
    api.MapScalarApiReference(options =>
    {
        options.Title = "ImageShare";
        options.Telemetry = false;
        options.Theme = ScalarTheme.Solarized;
        options.ShowDeveloperTools = DeveloperToolsVisibility.Never;
        options.Agent = new ScalarAgentOptions
        {
            Disabled = true
        };
        options.WithOpenApiRoutePattern("/api/openapi/{documentName}.json");
    });
}

app.UseCustomErrors();
app.UseHttpsRedirection();
app.UseRateLimiting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSpaHosting(app.Environment);

api.MapHealthEndpoints();
api.MapAuthEndpoints();
api.MapTokenEndpoints();
api.MapContentEndpoints();

await app.RunAsync();
