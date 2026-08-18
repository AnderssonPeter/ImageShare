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

        var keyStoragePath = configuration[$"{SectionName}:{nameof(DataProtectionOptions.KeyStoragePath)}"];
        if (string.IsNullOrWhiteSpace(keyStoragePath))
        {
            keyStoragePath = DataProtectionOptions.DefaultKeyStoragePath;
        }
#pragma warning disable RS0030
        var keyDirectory = Path.GetFullPath(keyStoragePath);
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
