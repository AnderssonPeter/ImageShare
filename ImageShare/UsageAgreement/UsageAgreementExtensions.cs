using Microsoft.Extensions.DependencyInjection;

namespace ImageShare.UsageAgreement;

public static class UsageAgreementExtensions
{
    public static IServiceCollection AddUsageAgreement(this IServiceCollection services)
    {
        services.AddOptions<UsageAgreementOptions>()
            .BindConfiguration("UsageAgreement")
            .Validated();

        services.AddScoped<IUsageAgreement, UsageAgreementConsent>();

        return services;
    }
}
