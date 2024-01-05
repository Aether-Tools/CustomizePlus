using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace CustomizePlus.Core.Helpers;

public static class Base64Helper
{
    // Compress any type to a base64 encoding of its compressed json representation, prepended with a version byte.
    // Returns an empty string on failure.
    // Original by Ottermandias: OtterGui <3
    public static unsafe string ExportToBase64<T>(T obj, byte version)
    {
        try
        {
            var json = JsonConvert.SerializeObject(obj, Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(json);
            using var compressedStream = new MemoryStream();
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(new ReadOnlySpan<byte>(&version, 1));
                zipStream.Write(bytes, 0, bytes.Length);
            }

            return Convert.ToBase64String(compressedStream.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }

    // Decompress a base64 encoded string to the given type and a prepended version byte if possible.
    // On failure, data will be String error and version will be byte.MaxValue.
    // Original by Ottermandias: OtterGui <3
    public static byte ImportFromBase64(string base64, out string data)
    {
        var version = byte.MaxValue;
        try
        {
            var bytes = Convert.FromBase64String(base64);
            using var compressedStream = new MemoryStream(bytes);
            using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream = new MemoryStream();
            zipStream.CopyTo(resultStream);
            bytes = resultStream.ToArray();
            version = bytes[0];
            var json = Encoding.UTF8.GetString(bytes, 1, bytes.Length - 1);
            data = json;
        }
        catch
        {
            data = "error";
        }

        return version;
    }
}