using PuzzAPI.Data.Services;

namespace PuzzAPI.Endpoints;

public class HttpUtils
{
    public static void SetTokenCookies(HttpResponse response, UserTokens tokens, bool secure)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTime.UtcNow.AddMinutes(15)
        };

        response.Cookies.Append("token", tokens.AuthToken, cookieOptions);
        cookieOptions.Expires = DateTime.UtcNow.AddDays(15);
        response.Cookies.Append("refreshToken", tokens.RefreshToken, cookieOptions);
    }
}