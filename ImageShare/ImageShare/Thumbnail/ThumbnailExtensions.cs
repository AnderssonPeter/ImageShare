namespace ImageShare.Thumbnail;

public static class ThumbnailExtensions
{
    public static IServiceCollection AddThumbnailService(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var thumbnailOptions = configuration?.GetSection("Thumbnail").Get<ThumbnailOptions>() ?? new ThumbnailOptions();
        services.AddSingleton<IThumbnailService>(new ThumbnailService(thumbnailOptions));

        var thumbprintOptions = configuration?.GetSection("Thumbprint").Get<ThumbprintOptions>() ?? new ThumbprintOptions();
        services.Configure<ThumbprintOptions>(options =>
        {
            options.ThumbSuffix = thumbprintOptions.ThumbSuffix;
            options.ThumbFormat = thumbprintOptions.ThumbFormat;
            options.WatchForChanges = thumbprintOptions.WatchForChanges;
            options.MaxConcurrentGenerations = thumbprintOptions.MaxConcurrentGenerations;
        });
        //services.AddHostedService<ThumbprintService>();

        return services;
    }
}
