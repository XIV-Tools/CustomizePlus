// © Customize+.
// Licensed under the MIT license.

using System;
using System.Numerics;
using CustomizePlus.Anamnesis;
using CustomizePlus.Memory;

namespace CustomizePlus.Extensions
{
    internal static class VectorExtensions
    {
        public static bool IsApproximately(this HkVector4 vector, Vector3 other, float errorMargin = 0.001f)
        {
            return IsApproximately(vector.X, other.X, errorMargin)
                   && IsApproximately(vector.Y, other.Y, errorMargin)
                   && IsApproximately(vector.Z, other.Z, errorMargin);
        }

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

        public static Quaternion ToQuaternion(this Vector4 rotation)
        {
            return new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
        }

        public static Quaternion ToQuaternion(this HkVector4 rotation)
        {
            return new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
        }

        public static HkVector4 ToHavokVector(this Quaternion rotation)
        {
            return new HkVector4(rotation.X, rotation.Y, rotation.Z, rotation.W);
        }

        public static HkVector4 ToHavokVector(this Vector4 vec)
        {
            return new HkVector4(vec.X, vec.Y, vec.Z, vec.W);
        }

        public static Vector3 GetAsNumericsVector(this PoseFile.Vector vec)
        {
            return new Vector3(vec.X, vec.Y, vec.Z);
        }
    }
}