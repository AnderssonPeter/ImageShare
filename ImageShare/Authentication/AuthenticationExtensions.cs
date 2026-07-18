using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace ImageShare.Authentication;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddUser(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        return services.AddScoped<IUser, User>();
    }

    public static IServiceCollection AddImageShareFilter(this IServiceCollection services) =>
        services.AddSingleton<ImageShareFilterService>();

    public static IServiceCollection AddOpenIdConnectAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OidcSettings>()
            .BindConfiguration("OpenIdConnect")
            .Validated();

        var oidcSettings = configuration.GetSection("OpenIdConnect").Get<OidcSettings>() ?? throw new InvalidDataException("Failed to get open id settings");

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddOpenIdConnect(options =>
        {
            options.Authority = oidcSettings.Authority;
            options.ClientId = oidcSettings.ClientId;
            options.ClientSecret = oidcSettings.ClientSecret;
            options.ResponseType = oidcSettings.ResponseType;
            options.SaveTokens = false;
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("image_share_filter");
            options.CallbackPath = "/signin-oidc";
            options.SignedOutCallbackPath = "/signout-callback-oidc";
            options.UsePkce = true;
        });

        services.AddAuthorization();

        return services;
    }
}
