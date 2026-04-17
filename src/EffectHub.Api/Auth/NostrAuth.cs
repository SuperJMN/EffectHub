using NBitcoin.Secp256k1;
using SysSHA256 = System.Security.Cryptography.SHA256;

namespace EffectHub.Api.Auth;

/// <summary>
/// Verifies Nostr Schnorr signatures for API mutations.
/// Headers: X-Nostr-PubKey (hex), X-Nostr-Signature (hex), X-Nostr-Timestamp (unix seconds).
/// Message = "{method}:{path}:{timestamp}"
/// </summary>
public static class NostrAuth
{
    private static readonly TimeSpan MaxTimestampDrift = TimeSpan.FromMinutes(5);

    public record VerificationResult(bool IsValid, string? PubKey = null, string? Error = null);

    public static VerificationResult Verify(HttpContext context)
    {
        var pubKeyHex = context.Request.Headers["X-Nostr-PubKey"].FirstOrDefault();
        var signatureHex = context.Request.Headers["X-Nostr-Signature"].FirstOrDefault();
        var timestampStr = context.Request.Headers["X-Nostr-Timestamp"].FirstOrDefault();

        if (string.IsNullOrEmpty(pubKeyHex) || string.IsNullOrEmpty(signatureHex) || string.IsNullOrEmpty(timestampStr))
            return new(false, Error: "Missing authentication headers (X-Nostr-PubKey, X-Nostr-Signature, X-Nostr-Timestamp).");

        if (!long.TryParse(timestampStr, out var timestamp))
            return new(false, Error: "Invalid timestamp.");

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - timestamp) > (long)MaxTimestampDrift.TotalSeconds)
            return new(false, Error: "Timestamp too far from server time.");

        var message = $"{context.Request.Method}:{context.Request.Path}:{timestamp}";
        var messageHash = SysSHA256.HashData(System.Text.Encoding.UTF8.GetBytes(message));

        byte[] pubKeyBytes, sigBytes;
        try
        {
            pubKeyBytes = Convert.FromHexString(pubKeyHex);
            sigBytes = Convert.FromHexString(signatureHex);
        }
        catch
        {
            return new(false, Error: "Invalid hex encoding in headers.");
        }

        if (pubKeyBytes.Length != 32 || sigBytes.Length != 64)
            return new(false, Error: "Invalid key or signature length.");

        try
        {
            if (!ECXOnlyPubKey.TryCreate(pubKeyBytes, out var xOnlyPubKey))
                return new(false, Error: "Invalid public key.");

            if (!SecpSchnorrSignature.TryCreate(sigBytes, out var signature))
                return new(false, Error: "Invalid signature format.");

            if (!xOnlyPubKey.SigVerifyBIP340(signature, messageHash))
                return new(false, Error: "Signature verification failed.");

            return new(true, PubKey: pubKeyHex.ToLowerInvariant());
        }
        catch
        {
            return new(false, Error: "Signature verification error.");
        }
    }
}
