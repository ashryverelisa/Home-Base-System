using Microsoft.AspNetCore.Localization;

namespace HomeBase.Localization;

public static class CultureEndpoints
{
    public const string SetCulturePath = "/culture/set";

    public static IEndpointRouteBuilder MapCultureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
            SetCulturePath,
            (HttpContext http, string? culture, string? redirectUri) =>
            {
                if (SupportedCultures.IsSupported(culture))
                {
                    http.Response.Cookies.Append(
                        CookieRequestCultureProvider.DefaultCookieName,
                        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture!)),
                        new CookieOptions
                        {
                            Path = "/",
                            Expires = DateTimeOffset.UtcNow.AddYears(1),
                            HttpOnly = true,
                            SameSite = SameSiteMode.Lax,
                            IsEssential = true,
                        }
                    );
                }

                return Results.Redirect(LocalOrHome(redirectUri));
            }
        );

        return endpoints;
    }

    private static string LocalOrHome(string? redirectUri)
    {
        if (string.IsNullOrEmpty(redirectUri) || redirectUri[0] != '/')
        {
            return "/";
        }

        return redirectUri.Length > 1 && redirectUri[1] is '/' or '\\' ? "/" : redirectUri;
    }
}
