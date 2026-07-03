namespace ImageShare.Thumbnail;

public static class ThumbnailExtensions
{
    public static IServiceCollection AddThumbnailService(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var options = configuration?.GetSection("Thumbnail").Get<ThumbnailOptions>() ?? new ThumbnailOptions();
        return services.AddSingleton<IThumbnailService>(new ThumbnailService(options));
    }
}
