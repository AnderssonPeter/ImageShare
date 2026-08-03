using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ImageShare.Browsing;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ImageShare;

public static class OpenApiExtensions
{
    public static IServiceCollection AddImageShareOpenApi(this IServiceCollection services) =>
        services.AddOpenApi(options =>
        {
            options.CreateSchemaReferenceId = CreateSchemaReferenceId;
            options.AddSchemaTransformer(async (schema, context, cancellationToken) =>
            {
                if (context.JsonTypeInfo.Type == typeof(RelativePath))
                {
                    schema.Type = JsonSchemaType.String;
                    schema.Properties = null;
                    schema.Required = null;
                }

                await Task.CompletedTask;
            });
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                if (string.IsNullOrEmpty(operation.OperationId))
                {
                    var methodInfo = context.Description.ActionDescriptor.EndpointMetadata.OfType<MethodInfo>().FirstOrDefault();
                    if (methodInfo is not null)
                    {
                        operation.OperationId = methodInfo.Name;
                        if (operation.OperationId.EndsWith("Async", StringComparison.Ordinal))
                        {
                            operation.OperationId = operation.OperationId[..^5];
                        }
                    }
                }

                if (operation.Parameters is null)
                {
                    return Task.CompletedTask;
                }

                foreach (var parameter in operation.Parameters)
                {
                    if (parameter is not OpenApiParameter queryParameter || queryParameter.In is ParameterLocation.Path)
                    {
                        continue;
                    }

                    var name = queryParameter.Name;
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    queryParameter.Name = JsonNamingPolicy.CamelCase.ConvertName(name);
                }

                return Task.CompletedTask;
            });
        });

    private static string? CreateSchemaReferenceId(JsonTypeInfo typeInfo)
    {
        var referenceId = OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
        if (referenceId is null || referenceId.Length < 2)
        {
            return referenceId;
        }

        var type = typeInfo.Type;
        if (type.IsInterface && referenceId[0] == 'I' && char.IsUpper(referenceId[1]))
        {
            return referenceId[1..];
        }

        return referenceId;
    }
}
