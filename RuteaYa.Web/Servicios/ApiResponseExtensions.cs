using System.Net.Http.Json;

namespace RuteaYa.Web.Servicios;

internal record ApiError(int? Status, string? Title, string? Detail);

public static class ApiResponseExtensions
{
    /// <summary>
    /// Si la API respondió con error, lanza una <see cref="ApiException"/> con el mensaje del servidor.
    /// </summary>
    public static async Task EnsureApiSuccessAsync(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var mensaje = "No se pudo completar la operación.";
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            if (!string.IsNullOrWhiteSpace(error?.Detail))
            {
                mensaje = error.Detail;
            }
            else if (!string.IsNullOrWhiteSpace(error?.Title))
            {
                mensaje = error.Title;
            }
        }
        catch (Exception)
        {
            // cuerpo vacío o que no es JSON: queda el mensaje genérico
        }

        throw new ApiException((int)response.StatusCode, mensaje);
    }
}
