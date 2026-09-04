using CustomizePlus.Core.Data;

namespace CustomizePlus.Templates;

internal sealed class TemplateEditorSnapshot : IEquatable<TemplateEditorSnapshot>
{
    public IReadOnlyDictionary<string, BoneTransformState> Bones { get; }

    private TemplateEditorSnapshot(Dictionary<string, BoneTransformState> bones)
    {
        Bones = bones;
    }

    public static TemplateEditorSnapshot Capture(IReadOnlyDictionary<string, BoneTransform> bones)
    {
        return new TemplateEditorSnapshot(bones.ToDictionary(
            x => x.Key,
            x => BoneTransformState.Capture(x.Value)));
    }

    public Dictionary<string, BoneTransform> CreateTransforms()
        => Bones.ToDictionary(x => x.Key, x => x.Value.CreateTransform());

    public bool Equals(TemplateEditorSnapshot? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other == null || Bones.Count != other.Bones.Count)
            return false;

        return Bones.All(x => other.Bones.TryGetValue(x.Key, out var value) && value == x.Value);
    }

    public override bool Equals(object? obj)
        => obj is TemplateEditorSnapshot other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var bone in Bones.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            hash.Add(bone.Key, StringComparer.Ordinal);
            hash.Add(bone.Value);
        }

        return hash.ToHashCode();
    }
}

internal readonly record struct BoneTransformState(
    Vector3 Translation,
    Vector3 Rotation,
    Vector3 Scaling,
    Vector3 ChildScaling,
    bool PropagateTranslation,
    bool PropagateRotation,
    bool PropagateScale,
    bool ChildScalingIndependent)
{
    public static BoneTransformState Capture(BoneTransform transform)
        => new(
            transform.Translation,
            transform.Rotation,
            transform.Scaling,
            transform.ChildScaling,
            transform.PropagateTranslation,
            transform.PropagateRotation,
            transform.PropagateScale,
            transform.ChildScalingIndependent);

    public BoneTransform CreateTransform()
        => new()
        {
            Translation = Translation,
            Rotation = Rotation,
            Scaling = Scaling,
            ChildScaling = ChildScaling,
            PropagateTranslation = PropagateTranslation,
            PropagateRotation = PropagateRotation,
            PropagateScale = PropagateScale,
            ChildScalingIndependent = ChildScalingIndependent,
        };
}
