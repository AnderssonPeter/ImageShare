using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ImageShare;

public static class OptionsBuilderExtensions
{
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Options types are annotated with DynamicallyAccessedMembers.")]
    public static OptionsBuilder<TOptions> Validated<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    TOptions
    >(this OptionsBuilder<TOptions> builder) where TOptions : class =>
        builder.ValidateDataAnnotations().ValidateOnStart();
}
