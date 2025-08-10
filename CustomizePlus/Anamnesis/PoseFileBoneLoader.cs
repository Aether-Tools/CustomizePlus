using CustomizePlusPlus.Anamnesis.Data;
using CustomizePlusPlus.Core.Data;
using CustomizePlusPlus.Core.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace CustomizePlusPlus.Anamnesis;

public class PoseFileBoneLoader
{
    public Dictionary<string, BoneTransform>? LoadBoneTransformsFromFile(string path)
    {
        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);

        JsonSerializerSettings settings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter> { new PoseFile.VectorConverter() }
        };

        var pose = JsonConvert.DeserializeObject<PoseFile>(json, settings);

        if (pose == null)
        {
            throw new Exception("Failed to deserialize pose file");
        }

        if (pose.Bones == null)
        {
            return null;
        }

        var retDict = new Dictionary<string, BoneTransform>();

        foreach (var kvp in pose.Bones)
        {
            if (kvp.Key == Constants.RootBoneName || kvp.Value == null || kvp.Value.Scale == null)
                continue;

            var scale = kvp.Value!.Scale!.GetAsNumericsVector();
            if (scale == Vector3.One)
                continue;

            retDict[kvp.Key] = new BoneTransform
            {
                Scaling = scale
            };
        }

        //load up root, but check it more rigorously

        var validRoot = pose.Bones.TryGetValue(Constants.RootBoneName, out var root)
                        && root != null
                        && root.Scale != null
                        && root.Scale.GetAsNumericsVector() != Vector3.Zero
                        && root.Scale.GetAsNumericsVector() != Vector3.One;

        if (validRoot)
        {
            retDict[Constants.RootBoneName] = new BoneTransform
            {
                Scaling = root!.Scale!.GetAsNumericsVector()
            };
        }

        return retDict;
    }
}
