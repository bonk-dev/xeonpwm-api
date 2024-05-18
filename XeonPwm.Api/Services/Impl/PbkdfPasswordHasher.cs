using System.Security.Cryptography;
using System.Text;

namespace XeonPwm.Api.Services.Impl;

public class PbkdfPasswordHasher : IPasswordHasher
{
    private readonly IConfiguration _configuration;

    public PbkdfPasswordHasher(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public string Hash(string password)
    {
        var iterationCount = _configuration.GetRequiredSection("Authentication").GetValue<int>("HashIteration");

        Span<byte> concat = stackalloc byte[16 + 64];
        RandomNumberGenerator.Fill(concat[..16]);
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), 
            concat[..16],
            concat[16..], 
            iterationCount,
            HashAlgorithmName.SHA3_512);

        return Convert.ToHexString(concat);
    }

    public bool Verify(string password, string hash)
    {
        var iterationCount = _configuration.GetRequiredSection("Authentication").GetValue<int>("HashIteration");
        Span<byte> originalHashBytes = Convert.FromHexString(hash);
        Span<byte> calcHash = stackalloc byte[64];
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password), 
            originalHashBytes[..16],
            calcHash, 
            iterationCount,
            HashAlgorithmName.SHA3_512);

        return originalHashBytes[16..].SequenceEqual(calcHash);
    }
}