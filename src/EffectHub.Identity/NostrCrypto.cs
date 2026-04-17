using NBitcoin.Secp256k1;
using SysSHA256 = System.Security.Cryptography.SHA256;

namespace EffectHub.Identity;

/// <summary>
/// Low-level Nostr cryptographic operations: key generation, Schnorr signing (BIP-340), verification.
/// </summary>
public static class NostrCrypto
{
    public static NostrKeyPair GenerateKeyPair()
    {
        var privateKeyBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return CreateKeyPair(privateKeyBytes);
    }

    public static NostrKeyPair CreateKeyPair(byte[] privateKeyBytes)
    {
        if (privateKeyBytes.Length != 32)
            throw new ArgumentException("Private key must be 32 bytes.", nameof(privateKeyBytes));

        var ecPrivKey = ECPrivKey.Create(privateKeyBytes);
        var xOnlyPubKey = ecPrivKey.CreateXOnlyPubKey();
        var pubKeyBytes = new byte[32];
        xOnlyPubKey.WriteToSpan(pubKeyBytes);

        return new NostrKeyPair(privateKeyBytes, pubKeyBytes);
    }

    public static NostrKeyPair FromNsec(string nsec)
    {
        var (hrp, data) = Bech32.Decode(nsec);
        if (hrp != "nsec")
            throw new ArgumentException("Invalid nsec string: wrong HRP.", nameof(nsec));
        if (data.Length != 32)
            throw new ArgumentException("Invalid nsec string: wrong data length.", nameof(nsec));
        return CreateKeyPair(data);
    }

    public static byte[] SignSchnorr(byte[] privateKey, byte[] messageHash)
    {
        if (messageHash.Length != 32)
            throw new ArgumentException("Message hash must be 32 bytes.", nameof(messageHash));

        var ecPrivKey = ECPrivKey.Create(privateKey);
        var signature = ecPrivKey.SignBIP340(messageHash);
        var sigBytes = new byte[64];
        signature.WriteToSpan(sigBytes);
        return sigBytes;
    }

    public static bool VerifySchnorr(byte[] publicKey, byte[] messageHash, byte[] signature)
    {
        if (publicKey.Length != 32 || messageHash.Length != 32 || signature.Length != 64)
            return false;

        try
        {
            if (!ECXOnlyPubKey.TryCreate(publicKey, out var xOnlyPubKey))
                return false;

            if (!SecpSchnorrSignature.TryCreate(signature, out var secpSig))
                return false;

            return xOnlyPubKey.SigVerifyBIP340(secpSig, messageHash);
        }
        catch
        {
            return false;
        }
    }

    public static byte[] HashMessage(string message)
    {
        return SysSHA256.HashData(System.Text.Encoding.UTF8.GetBytes(message));
    }
}
