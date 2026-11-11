using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RuteaYa.Api.Swagger;

/// <summary>
/// Marca con candado en Swagger solo los endpoints que exigen token ([Authorize] sin [AllowAnonymous]),
/// así registro, login y health se pueden probar sin autenticarse.
/// </summary>
public sealed class AutorizacionOperationFilter : IOperationFilter
{
    public const string Esquema = "Bearer";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var requiereToken = metadata.OfType<IAuthorizeData>().Any()
            && !metadata.OfType<IAllowAnonymous>().Any();

        if (!requiereToken)
        {
            return;
        }

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = Esquema }
            }] = []
        });

        operation.Responses.TryAdd(
            StatusCodes.Status401Unauthorized.ToString(),
            new OpenApiResponse { Description = "Falta el token o es inválido." });
    }
}
