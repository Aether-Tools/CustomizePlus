using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace CustomizePlus.Anamnesis.Data;

[Serializable]
public class PoseFile
{
    public Vector? Scale { get; set; }

    public Dictionary<string, Bone?>? Bones { get; set; }

    [Serializable]
    public class Bone
    {
        public Vector? Scale { get; set; }
    }

    [Serializable]
    public class Vector
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public static Vector FromString(string str)
        {
            var parts = str.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
            {
                throw new FormatException();
            }

            Vector v = new()
            {
                X = float.Parse(parts[0], CultureInfo.InvariantCulture),
                Y = float.Parse(parts[1], CultureInfo.InvariantCulture),
                Z = float.Parse(parts[2], CultureInfo.InvariantCulture)
            };
            return v;
        }

        public override string ToString()
        {
            return $"{X}, {Y}, {Z}";
        }
    }

    public class VectorConverter : JsonConverter<Vector>
    {
        public override Vector? ReadJson(JsonReader reader, Type objectType, Vector? existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            return reader.Value is not string str ? null : Vector.FromString(str);
        }

        public override void WriteJson(JsonWriter writer, Vector? value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }
    }
}