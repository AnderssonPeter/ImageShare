using ImageShare.Browsing;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.Tests;

public class StaticAnalysis
{
    private static readonly Type[] bindingAttributes =
    [
        typeof(FromQueryAttribute),
        typeof(FromRouteAttribute),
        typeof(FromBodyAttribute),
        typeof(FromHeaderAttribute),
        typeof(FromServicesAttribute),
    ];

    public static IEnumerable<Type> MessageTypes =>
        typeof(GetEntriesQuery).Assembly.GetTypes()
            .Where(type => (typeof(IBaseQuery).IsAssignableFrom(type) || typeof(IBaseCommand).IsAssignableFrom(type))
                           && type is { IsClass: true, IsAbstract: false });

    [Test]
    [MethodDataSource(nameof(MessageTypes))]
    public async Task Query_AllParametersHaveBindingSourceAttribute(Type queryType)
    {
        // Arrange
        var constructor = queryType.GetConstructors().FirstOrDefault();
        var violations = new List<string>();

        // Act
        if (constructor is null)
        {
            violations.Add($"{queryType.Name}: no public constructor found");
        }
        else
        {
            foreach (var parameter in constructor.GetParameters())
            {
                var hasBindingAttribute = bindingAttributes.Any(attr => parameter.IsDefined(attr, false));

                if (!hasBindingAttribute)
                {
                    violations.Add($"{queryType.Name}: parameter '{parameter.Name}' is missing a binding source attribute ([FromQuery], [FromRoute], [FromBody], [FromHeader], or [FromServices])");
                }
            }
        }

        // Assert
        await Assert.That(string.Join("\n", violations)).IsEmpty();
    }
}
