using ImageShare.Authentication;
using ImageShare.Browsing;
using ImageShare.ImageConversion;
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
    private static readonly IServiceProvider ServiceProvider = BuildProvider();

    public override IServiceScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata) =>
        ServiceProvider.CreateScope();

    public override object? Create(IServiceScope scope, Type type) =>
        scope.ServiceProvider.GetService(type);

    private static IServiceProvider BuildProvider()
    {
        var contentTypeProvider = new FileExtensionContentTypeProvider();
        contentTypeProvider.Mappings[".avif"] = "image/avif";

        return new ServiceCollection()
            .AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped)
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
            .AddSingleton<ImageShareFilterService>()
            .AddTransient<ImageConverterJob>()
            .AddSingleton<TestImageFactory>()
            .AddScoped<TestUser>()
            .AddScoped<IUser>(sp => sp.GetRequiredService<TestUser>())
            .BuildServiceProvider();
    }
}
