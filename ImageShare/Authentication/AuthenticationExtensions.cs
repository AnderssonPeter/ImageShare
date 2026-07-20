using AspNetCore.Authentication.ApiKey;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace ImageShare.Authentication;

public static class AuthenticationExtensions
{
    public const string DefaultScheme = "Default";
    public const string ApiKeyHeaderName = "X-API-Key";

    public static IServiceCollection AddUser(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        return services.AddScoped<IUser, User>();
    }

    public static IServiceCollection AddImageShareFilter(this IServiceCollection services) =>
        services.AddSingleton<ImageShareFilterService>();

    public static IServiceCollection AddJwtTokenService(this IServiceCollection services) =>
        services.AddSingleton<JwtTokenService>();

    public static IServiceCollection AddOpenIdConnectAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OidcSettings>()
            .BindConfiguration("OpenIdConnect")
            .Validated();

        services.AddOptions<ApiKeySettings>()
            .BindConfiguration("ApiKeys")
            .Validated();

        services.AddOptions<JwtSettings>()
            .BindConfiguration("Jwt")
            .Validated();

        var oidcSettings = configuration.GetSection("OpenIdConnect").Get<OidcSettings>() ?? throw new InvalidDataException("Failed to get open id settings");

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = DefaultScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddPolicyScheme(DefaultScheme, null, options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                if (context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var headerApiKey) &&
                    !string.IsNullOrEmpty(headerApiKey))
                {
                    return ApiKeyDefaults.AuthenticationScheme;
                }

                if (context.Request.Query.TryGetValue(ApiKeyHeaderName, out var queryApiKey) &&
                    !string.IsNullOrEmpty(queryApiKey))
                {
                    return ApiKeyDefaults.AuthenticationScheme;
                }

                return CookieAuthenticationDefaults.AuthenticationScheme;
            };
        })
        .AddCookie()
        .AddOpenIdConnect(options => ConfigureOpenIdConnect(options, oidcSettings))
        .AddApiKeyInHeaderOrQueryParams<ApiKeyProvider>(options =>
        {
            options.Realm = "ImageShare";
            options.KeyName = ApiKeyHeaderName;
            options.SuppressWWWAuthenticateHeader = true;
        });

        services.AddAuthorization();

        return services;
    }

    private static void ConfigureOpenIdConnect(OpenIdConnectOptions options, OidcSettings oidcSettings)
    {
        options.Authority = oidcSettings.Authority;
        options.ClientId = oidcSettings.ClientId;
        options.ClientSecret = oidcSettings.ClientSecret;
        options.ResponseType = oidcSettings.ResponseType;
        options.SaveTokens = false;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add(ImageShareClaims.ImageShareFilter);
        options.Scope.Add(ImageShareClaims.Roles);
        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
        options.UsePkce = true;
        options.ClaimActions.MapUniqueJsonKey(ImageShareClaims.Role, ImageShareClaims.Role);
        options.ClaimActions.MapUniqueJsonKey(ImageShareClaims.Role, ImageShareClaims.Roles);
        options.TokenValidationParameters.RoleClaimType = ImageShareClaims.Role;
    }
}
