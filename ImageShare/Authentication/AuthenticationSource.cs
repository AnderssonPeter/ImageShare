using System.Text.Json.Serialization;

namespace ImageShare.Authentication;

[JsonConverter(typeof(JsonStringEnumConverter<AuthenticationSource>))]
public enum AuthenticationSource
{
    None,
    OpenIdConnect,
    ApiKey,
    Jwt,
}
