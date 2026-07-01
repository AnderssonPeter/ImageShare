using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ImageShare.Authentication;

public class OidcSettings
{
    public string Authority { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string ResponseType { get; set; } = OpenIdConnectResponseType.Code;
}
