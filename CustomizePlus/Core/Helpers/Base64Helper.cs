using CustomizePlus.Core.Data;
using CustomizePlus.Templates.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;

namespace CustomizePlus.Core.Helpers;
public class BoneTransformData // literally not cooking
{
    public string BoneCodeName { get; set; }
    public Vector3 Translation { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 Scaling { get; set; }
    public Vector3 ChildScaling { get; set; }
    public bool ChildScalingIndependent { get; set; }
    public bool PropagateTranslation { get; set; }
    public bool PropagateRotation { get; set; }
    public bool PropagateScale { get; set; }
}

//this is jank but I don't have time to rewrite it
public static class Base64Helper
{
    // Compress any type to a base64 encoding of its compressed json representation, prepended with a version byte.
    // Returns an empty string on failure.
    // Original by Ottermandias: OtterGui <3
    public static string ExportTemplateToBase64(Template template)
    {
        try
        {
            var json = template.JsonSerialize();
            var bytes = Encoding.UTF8.GetBytes(json.ToString(Formatting.None));
            using var compressedStream = new MemoryStream();
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.WriteByte(Template.Version);
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

    public static string ExportEditedBonesToBase64(IEnumerable<(string BoneCodeName, BoneTransform Transform)> bones)
    {
        try
        {
            var DataList = bones.Select(b => new BoneTransformData
            {
                BoneCodeName = b.BoneCodeName,
                Translation = b.Transform.Translation,
                Rotation = b.Transform.Rotation,
                Scaling = b.Transform.Scaling,
                ChildScaling = b.Transform.ChildScaling,
                ChildScalingIndependent = b.Transform.ChildScalingIndependent,
                PropagateTranslation = b.Transform.PropagateTranslation,
                PropagateRotation = b.Transform.PropagateRotation,
                PropagateScale = b.Transform.PropagateScale
            }).ToList(); // dont let me cook

            var json = JsonConvert.SerializeObject(DataList, Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var compressedStream = new MemoryStream();
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.WriteByte(1);
                zipStream.Write(bytes, 0, bytes.Length);
            }

            return Convert.ToBase64String(compressedStream.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }

    public static List<BoneTransformData> ImportEditedBonesFromBase64(string base64)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64);

            using var compressedStream = new MemoryStream(bytes);
            using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);

            _ = zipStream.ReadByte();

            using var resultStream = new MemoryStream();
            zipStream.CopyTo(resultStream);

            var json = Encoding.UTF8.GetString(resultStream.ToArray());
            return JsonConvert.DeserializeObject<List<BoneTransformData>>(json);
        }
        catch
        {
            return null;
        }
    }
}