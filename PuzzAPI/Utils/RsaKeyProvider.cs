using Microsoft.IdentityModel.Tokens;

namespace PuzzAPI.Utils;

public class RsaKeyProvider
{
    public RsaKeyProvider(RsaSecurityKey key)
    {
        RsaSecurityKey = key;
    }

    public RsaSecurityKey RsaSecurityKey { get; }
}