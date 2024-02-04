using System;
using System.Numerics;

namespace CustomizePlus.Configuration.Data.Version2;

[Serializable]
public struct V2BoneEditsContainer
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Rotation { get; set; } = Vector3.Zero;
    public Vector3 Scale { get; set; } = Vector3.One;

    public V2BoneEditsContainer()
    {
    }
}
