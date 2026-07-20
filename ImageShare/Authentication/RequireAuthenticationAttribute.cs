namespace ImageShare.Authentication;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public sealed class RequireAuthenticationAttribute : Attribute;
