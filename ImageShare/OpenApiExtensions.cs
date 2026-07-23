using System.Text.Json;
using ImageShare.Browsing;
using Microsoft.OpenApi;

namespace ImageShare;

public static class OpenApiExtensions
{
    public static IServiceCollection AddImageShareOpenApi(this IServiceCollection services) =>
        services.AddOpenApi(options =>
        {
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
}
