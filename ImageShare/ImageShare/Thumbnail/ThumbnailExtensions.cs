namespace ImageShare.Thumbnail;

public static class ThumbnailExtensions
{
    public static IServiceCollection AddThumbnailService(this IServiceCollection services)
    {
        services.AddOptions<ThumbnailOptions>()
            .BindConfiguration("Thumbnail")
            .Validated();

        services.AddSingleton<IThumbnailService, ThumbnailService>();

        services.AddOptions<ThumbprintOptions>()
            .BindConfiguration("Thumbprint")
            .Validated();

        return services;
    }
}
