using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ImageShare.Authentication;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class OidcSettings
{
    [Required]
    public required string Authority { get; set; }

    [Required]
    public required string ClientId { get; set; }

    [Required]
    public required string ClientSecret { get; set; }

    public string ResponseType { get; set; } = OpenIdConnectResponseType.Code;
}
