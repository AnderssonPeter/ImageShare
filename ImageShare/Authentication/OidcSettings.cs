using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ImageShare.Authentication;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class OidcSettings
{
    [Required]
    public string Authority { get; set; } = "";

    [Required]
    public string ClientId { get; set; } = "";

    [Required]
    public string ClientSecret { get; set; } = "";

    public string ResponseType { get; set; } = OpenIdConnectResponseType.Code;

    [Required]
    public string AdminRole { get; set; } = "admin";
}
