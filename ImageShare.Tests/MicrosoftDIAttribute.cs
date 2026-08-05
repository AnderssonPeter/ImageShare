using ImageShare.Authentication;
using ImageShare.Browsing;
using ImageShare.ImageConversion;
using ImageShare.UsageAgreement;
using Mediator;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mirality.FileProviders;
using Mirality.FileProviders.InMemory;

namespace ImageShare.Tests;

public class MicrosoftDIAttribute : DependencyInjectionDataSourceAttribute<IServiceScope>
{
    private static readonly IServiceProvider serviceProvider = BuildProvider();

    public override IServiceScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata) =>
        serviceProvider.CreateScope();

    public override object? Create(IServiceScope scope, Type type) =>
        scope.ServiceProvider.GetService(type);

    private static IServiceProvider BuildProvider()
    {
        var contentTypeProvider = new FileExtensionContentTypeProvider();
        contentTypeProvider.Mappings[".avif"] = "image/avif";

        return new ServiceCollection()
            .AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped)
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthenticationBehavior<,>))
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(AdminBehavior<,>))
            .AddScoped<ImageEnumerator>()
            .AddScoped<InMemoryFileProvider>()
            .AddScoped<ISyncWritableFileProvider>(sp => sp.GetRequiredService<InMemoryFileProvider>())
            .AddScoped<IWritableFileProvider>(sp => sp.GetRequiredService<InMemoryFileProvider>())
            .AddScoped<IFileProvider>(sp => sp.GetRequiredService<InMemoryFileProvider>())
            .AddSingleton<IOptions<ImageFormatOptions>>(
                Options.Create(new ImageFormatOptions
                {
                    SupportedFormats = ["avif", "webp", "jpg", "png"]
                }))
            .AddSingleton<IOptions<ImageConverterOptions>>(
                Options.Create(new ImageConverterOptions
                {
                    FullQuality = 80,
                    ThumbnailQuality = 70,
                    ThumbnailMaxWidth = 200,
                    ThumbnailMaxHeight = 200,
                }))
            .AddSingleton<IContentTypeProvider>(contentTypeProvider)
            .AddSingleton<ILoggerFactory>(new LoggerFactory())
            .AddSingleton<ImageConverter>()
            .AddSingleton<ImageShareFilterCompiler>()
            .AddSingleton<IOptions<JwtSettings>>(
                Options.Create(new JwtSettings
                {
                    Issuer = "ImageShare",
                    Audience = "ImageShare",
                    SigningKey = "test-signing-key-must-be-at-least-32-characters-long",
                }))
            .AddSingleton<JwtTokenIssuer>()
            .AddSingleton<JwtTokenValidator>()
            .AddTransient<ImageConverterJob>()
            .AddSingleton<TestImageFactory>()
            .AddScoped<TestUser>()
            .AddScoped<IUser>(sp => sp.GetRequiredService<TestUser>())
            .AddScoped<TestUsageAgreement>()
            .AddScoped<IUsageAgreement>(sp => sp.GetRequiredService<TestUsageAgreement>())
            .BuildServiceProvider();
    }
}
