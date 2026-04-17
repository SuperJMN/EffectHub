namespace EffectHub.Identity;

public record NostrKeyPair(byte[] PrivateKey, byte[] PublicKey)
{
    public string PrivateKeyHex => Convert.ToHexString(PrivateKey).ToLowerInvariant();
    public string PublicKeyHex => Convert.ToHexString(PublicKey).ToLowerInvariant();
    public string Nsec => Bech32.Encode("nsec", PrivateKey);
    public string Npub => Bech32.Encode("npub", PublicKey);
}
