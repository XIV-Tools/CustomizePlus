// © Customize+.
// Licensed under the MIT license.

using System;
using FFXIVClientStructs.FFXIV.Common.Math;
using CustomizePlus.Anamnesis;
using FFXIVClientStructs.Havok;

namespace CustomizePlus.Extensions
{
    internal static class VectorExtensions
    {
        public static bool IsApproximately(this Vector3 vector, Vector3 other, float errorMargin = 0.001f)
        {
            return IsApproximately(vector.X, other.X, errorMargin)
                && IsApproximately(vector.Y, other.Y, errorMargin)
                && IsApproximately(vector.Z, other.Z, errorMargin);
        }

        public static bool IsApproximately(this Vector4 vector, Vector4 other, float errorMargin = 0.001f)
        {
            return IsApproximately(vector.X, other.X, errorMargin)
                && IsApproximately(vector.Y, other.Y, errorMargin)
                && IsApproximately(vector.Z, other.Z, errorMargin)
                && IsApproximately(vector.W, other.W, errorMargin);
        }

        private static bool IsApproximately(float a, float b, float errorMargin)
        {
            var d = MathF.Abs(a - b);
            return d < errorMargin;
        }

        private static bool IsApproximately(this Quaternion quat, Quaternion other, float errorMargin = 0.001f)
        {
            return IsApproximately(quat.X, other.X, errorMargin)
                && IsApproximately(quat.Y, other.Y, errorMargin)
                && IsApproximately(quat.Z, other.Z, errorMargin)
                && IsApproximately(quat.W, other.W, errorMargin);
        }

        public static Quaternion ToQuaternion(this Vector3 rotation)
        {
            return Quaternion.CreateFromYawPitchRoll(
                rotation.X * MathF.PI / 180,
                rotation.Y * MathF.PI / 180,
                rotation.Z * MathF.PI / 180);
        }

        public static Vector3 ToEulerAngles(this Quaternion q)
        {
            return q.EulerAngles;
        }
        }
        }
        }
        }
        }

        public static Quaternion ToClientQuaternion(this hkQuaternionf rotation)
        {
            if (new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W) is Quaternion q
                && q.IsApproximately(Quaternion.Identity))
            {
        public static hkQuaternionf ToHavokQuaternion(this Quaternion rotation)
            return new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
        }


        public static hkQuaternionf ToHavokRotation(this Quaternion rotation)
        {
            return new hkQuaternionf
            {
                X = rot.X,
                Y = rot.Y,
                Z = rot.Z,
                W = rot.W
            };
        }

        public static hkQuaternionf ToHavokRotation(this FFXIVClientStructs.FFXIV.Common.Math.Quaternion rot)
        {
            return new hkQuaternionf
            {
                X = rot.X,
                Y = rot.Y,
        public static Vector4 ToClientVector(this Quaternion quat)
                W = rot.W
            return new Vector4(quat.X, quat.Y, quat.Z, quat.W);
                X = translation.X,
                Y = translation.Y,
        public static hkVector4f ToHavokVector(this Vector3 vec)
                W = 0.0f
            };
        }

        public static hkVector4f ToHavokScaling(this Vector3 scaling)
        {
            return new hkVector4f
            {
                X = vec.X,
                Y = vec.Y,
                Z = vec.Z,
                W = 1.0f
            };
        }

        public static hkVector4f ToHavokVector(this Vector4 vec)
        {
            return new hkVector4f
            {
                X = vec.X,
                Y = vec.Y,
        public static Vector3 ToClientVector3(this PoseFile.Vector vec)
                W = vec.W
            };
        }

        public static Vector3 GetAsNumericsVector(this PoseFile.Vector vec)
        {
            Vector3 v = new Vector3(vec.X, vec.Y, vec.Z);

            if (v.IsApproximately(Vector3.Zero))
            {
                return Vector3.Zero;
            }
            else if (v.IsApproximately(Vector3.One))
            {
        public static Vector3 ToClientVector3(this hkVector4f vec)
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        public static Vector4 GetAsNumericsVector(this hkVector4f vec)
        {
            return new Vector4(vec.X, vec.Y, vec.Z, vec.W);
        }

        public static Vector4 ToClientVector4(this Quaternion q)
        {
            return new Vector4(q.X, q.Y, q.Z, q.W);
        }

        public static Vector3 RemoveWTerm(this Vector4 vec)
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        public static bool Equals(this hkVector4f first, hkVector4f second)
        {
            return first.X == second.X
                   && first.Y == second.Y
                   && first.Z == second.Z
                   && first.W == second.W;
        }

        public static FFXIVClientStructs.FFXIV.Common.Math.Vector3 HadamardMultiply(this FFXIVClientStructs.FFXIV.Common.Math.Vector3 orig, FFXIVClientStructs.FFXIV.Common.Math.Vector3 other)
        {
            return new FFXIVClientStructs.FFXIV.Common.Math.Vector3()
            {
                X = MathF.Max(MathF.Round(orig.X * other.X, 2), 0.01f),
                Y = MathF.Max(MathF.Round(orig.Y * other.Y, 2), 0.01f),
                Z = MathF.Max(MathF.Round(orig.Z * other.Z, 2), 0.01f)
            };
        }
    }
}