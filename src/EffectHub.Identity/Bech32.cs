namespace EffectHub.Identity;

/// <summary>
/// Minimal Bech32 encoder/decoder for Nostr npub/nsec encoding (NIP-19).
/// </summary>
public static class Bech32
{
    private const string Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";

    public static string Encode(string hrp, byte[] data)
    {
        var values = ConvertBits(data, 8, 5, true);
        var checksum = CreateChecksum(hrp, values);
        var combined = values.Concat(checksum).ToArray();
        return hrp + "1" + string.Concat(combined.Select(v => Charset[v]));
    }

    public static (string Hrp, byte[] Data) Decode(string bech32)
    {
        var pos = bech32.LastIndexOf('1');
        if (pos < 1 || pos + 7 > bech32.Length)
            throw new FormatException("Invalid bech32 string.");

        var hrp = bech32[..pos];
        var dataStr = bech32[(pos + 1)..];
        var data = dataStr.Select(c => (byte)Charset.IndexOf(c)).ToArray();

        if (data.Any(b => b == 255))
            throw new FormatException("Invalid character in bech32 data.");

        var checksum = VerifyChecksum(hrp, data);
        if (!checksum)
            throw new FormatException("Invalid bech32 checksum.");

        var decoded = ConvertBits(data[..^6], 5, 8, false);
        return (hrp, decoded);
    }

    private static byte[] ConvertBits(byte[] data, int fromBits, int toBits, bool pad)
    {
        var result = new List<byte>();
        int acc = 0, bits = 0;
        int maxv = (1 << toBits) - 1;

        foreach (var value in data)
        {
            acc = (acc << fromBits) | value;
            bits += fromBits;
            while (bits >= toBits)
            {
                bits -= toBits;
                result.Add((byte)((acc >> bits) & maxv));
            }
        }

        if (pad && bits > 0)
        {
            result.Add((byte)((acc << (toBits - bits)) & maxv));
        }

        return result.ToArray();
    }

    private static uint Polymod(byte[] values)
    {
        uint[] generator = [0x3b6a57b2u, 0x26508e6du, 0x1ea119fau, 0x3d4233ddu, 0x2a1462b3u];
        uint chk = 1;
        foreach (var v in values)
        {
            var top = chk >> 25;
            chk = ((chk & 0x1ffffffu) << 5) ^ v;
            for (int i = 0; i < 5; i++)
            {
                if (((top >> i) & 1) != 0)
                    chk ^= generator[i];
            }
        }
        return chk;
    }

    private static byte[] HrpExpand(string hrp)
    {
        var result = new byte[hrp.Length * 2 + 1];
        for (int i = 0; i < hrp.Length; i++)
        {
            result[i] = (byte)(hrp[i] >> 5);
            result[i + hrp.Length + 1] = (byte)(hrp[i] & 31);
        }
        result[hrp.Length] = 0;
        return result;
    }

    private static bool VerifyChecksum(string hrp, byte[] data)
    {
        var values = HrpExpand(hrp).Concat(data).ToArray();
        return Polymod(values) == 1;
    }

    private static byte[] CreateChecksum(string hrp, byte[] data)
    {
        var values = HrpExpand(hrp).Concat(data).Concat(new byte[6]).ToArray();
        var polymod = Polymod(values) ^ 1;
        var checksum = new byte[6];
        for (int i = 0; i < 6; i++)
        {
            checksum[i] = (byte)((polymod >> (5 * (5 - i))) & 31);
        }
        return checksum;
    }
}
