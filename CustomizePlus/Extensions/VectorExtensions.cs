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

        private static bool IsApproximately(float a, float b, float errorMargin)
        {
            var d = MathF.Abs(a - b);
            return d < errorMargin;
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

        public static Quaternion ToClientQuaternion(this hkQuaternionf rotation)
        {
            return new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
        }

        public static hkQuaternionf ToHavokQuaternion(this Quaternion rotation)
        {
            return new hkQuaternionf
            {
                X = rotation.X,
                Y = rotation.Y,
                Z = rotation.Z,
                W = rotation.W
            };
        }

        public static Vector4 ToClientVector(this Quaternion quat)
        {
            return new Vector4(quat.X, quat.Y, quat.Z, quat.W);
        }

        public static hkVector4f ToHavokVector(this Vector3 vec)
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
                Z = vec.Z,
                W = vec.W
            };
        }

        public static Vector3 ToClientVector3(this PoseFile.Vector vec)
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        public static Vector3 ToClientVector3(this hkVector4f vec)
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        public static Vector4 ToClientVector4(this hkVector4f vec)
        {
            return new Vector4(vec.X, vec.Y, vec.Z, vec.W);
        }

        public static Vector4 ToClientVector4(this Quaternion q)
        {
            return new Vector4(q.X, q.Y, q.Z, q.W);
        }

        public static bool Equals(this hkVector4f first, hkVector4f second)
        {
            return first.X == second.X
                   && first.Y == second.Y
                   && first.Z == second.Z
                   && first.W == second.W;
        }
    }
}