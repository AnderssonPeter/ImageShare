using Microsoft.Extensions.Options;

namespace ImageShare.ImageConversion;

public static class ImageConversionExtensions
{
    public static IServiceCollection AddImageConversion(this IServiceCollection services)
    {
        services.AddOptions<ImageConverterOptions>()
            .BindConfiguration("ImageConversion")
            .Validated();

        services.AddSingleton<ImageConverter>();
        services.AddHostedService<ImageConverterJob>();

        return services;
    }
}
