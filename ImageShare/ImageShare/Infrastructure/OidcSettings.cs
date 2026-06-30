using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ImageShare.Infrastructure;

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
