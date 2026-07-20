using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;

namespace ImageShare.Authentication;

internal sealed class ApiKey(string key, string ownerName, IReadOnlyCollection<Claim> claims) : IApiKey
{
    public string Key { get; } = key;
    public string OwnerName { get; } = ownerName;
    public IReadOnlyCollection<Claim> Claims { get; } = claims;
}
