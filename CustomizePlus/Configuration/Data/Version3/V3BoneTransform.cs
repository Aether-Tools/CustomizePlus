using System;
using System.Numerics;

namespace CustomizePlusPlus.Configuration.Data.Version3;

public class V3BoneTransform
{
    private Vector3 _translation;
    public Vector3 Translation
    {
        get => _translation;
        set => _translation = ClampVector(value);
    }

    private Vector3 _rotation;
    public Vector3 Rotation
    {
        get => _rotation;
        set => _rotation = ClampAngles(value);
    }

    private Vector3 _scaling;
    public Vector3 Scaling
    {
        get => _scaling;
        set => _scaling = ClampVector(value);
    }

    /// <summary>
    /// Clamp all vector values to be within allowed limits.
    /// </summary>
    private Vector3 ClampVector(Vector3 vector)
    {
        return new Vector3
        {
            X = Math.Clamp(vector.X, -512, 512),
            Y = Math.Clamp(vector.Y, -512, 512),
            Z = Math.Clamp(vector.Z, -512, 512)
        };
    }

    private static Vector3 ClampAngles(Vector3 rotVec)
    {
        static float Clamp(float angle)
        {
            if (angle > 180)
                angle -= 360;
            else if (angle < -180)
                angle += 360;

            return angle;
        }

        rotVec.X = Clamp(rotVec.X);
        rotVec.Y = Clamp(rotVec.Y);
        rotVec.Z = Clamp(rotVec.Z);

        return rotVec;
    }
}
