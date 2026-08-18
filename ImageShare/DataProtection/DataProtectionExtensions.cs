using Microsoft.AspNetCore.DataProtection;

namespace ImageShare.DataProtection;

public static class DataProtectionExtensions
{
    const string SectionName = "DataProtection";
    public static IServiceCollection AddDataProtectionKeys(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<DataProtectionOptions>()
            .BindConfiguration(SectionName)
            .Validated();

        var keyPath = configuration[$"{SectionName}:{nameof(DataProtectionOptions.KeyPath)}"];
        if (string.IsNullOrWhiteSpace(keyPath))
        {
            keyPath = DataProtectionOptions.DefaultKeyPath;
        }
#pragma warning disable RS0030
        var keyDirectory = Path.GetFullPath(keyPath);
        if (!Directory.Exists(keyDirectory))
        {
            Directory.CreateDirectory(keyDirectory);
        }
#pragma warning restore RS0030

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory));

        return services;
    }
}
