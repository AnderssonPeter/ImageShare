using AspNetCore.Authentication.ApiKey;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

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
        services.AddSingleton<ImageShareFilterCompiler>();

    public static IServiceCollection AddJwtTokens(this IServiceCollection services) =>
        services
            .AddSingleton<JwtTokenIssuer>()
            .AddSingleton<JwtTokenValidator>();

    public static IServiceCollection AddAuthentications(this IServiceCollection services)
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

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = DefaultScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddPolicyScheme(DefaultScheme, null, ConfigurePolicyScheme)
        .AddCookie(ConfigureCookie)
        .AddOpenIdConnect(ConfigureOpenIdConnect)
        .AddApiKeyInHeaderOrQueryParams<ApiKeyProvider>(ConfigureApiKey);

        services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, OpenIdConnectOptionsConfigure>();

        services.AddAuthorization();

        return services;
    }

    private static void ConfigurePolicyScheme(PolicySchemeOptions options)
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
    }

    private static void ConfigureCookie(CookieAuthenticationOptions options)
    {
        options.Events = new CookieAuthenticationEvents
        {
            OnSigningIn = context =>
            {
                if (context.Properties.ExpiresUtc.HasValue)
                {
                    context.Properties.IsPersistent = true;
                }

                return Task.CompletedTask;
            },
        };
    }

    private static void ConfigureApiKey(ApiKeyOptions options)
    {
        options.Realm = "ImageShare";
        options.KeyName = ApiKeyHeaderName;
        options.SuppressWWWAuthenticateHeader = true;
    }

    private static void ConfigureOpenIdConnect(OpenIdConnectOptions options)
    {
        options.SaveTokens = false;
        options.UseTokenLifetime = true;
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
