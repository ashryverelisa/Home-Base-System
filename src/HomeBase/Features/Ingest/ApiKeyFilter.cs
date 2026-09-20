using System.Security.Cryptography;
using System.Text;

namespace HomeBase.Features.Ingest;

public sealed class ApiKeyFilter(IConfiguration configuration) : IEndpointFilter
{
    public const string HeaderName = "X-Api-Key";
    public const string ConfigurationKey = "Ingest:ApiKey";
    public const string RateLimitPolicy = "ingest";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        if (configuration[ConfigurationKey] is not { Length: > 0 } expected)
        {
            return Results.Problem(
                "The ingest API key is not configured.",
                statusCode: StatusCodes.Status503ServiceUnavailable
            );
        }

        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

        return Matches(expected, provided) ? await next(context) : Results.Unauthorized();
    }

    private static bool Matches(string expected, string provided) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided)
        );
}
