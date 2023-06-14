// © Customize+.
// Licensed under the MIT license.

using System;
using System.Numerics;
using CustomizePlus.Anamnesis;
using FFXIVClientStructs.Havok;

namespace CustomizePlus.Extensions
{
    internal static class VectorExtensions
    {
        public static bool IsApproximately(this hkVector4f vector, Vector3 other, float errorMargin = 0.001f)
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
            var nq = Vector4.Normalize(q.GetAsNumericsVector());

            var rollX = MathF.Atan2(
                2 * (nq.W * nq.X + nq.Y * nq.Z),
                1 - 2 * (nq.X * nq.X + nq.Y * nq.Y));

            var pitchY = 2 * MathF.Atan2(
                MathF.Sqrt(1 + 2 * (nq.W * nq.Y - nq.X * nq.Z)),
                MathF.Sqrt(1 - 2 * (nq.W * nq.Y - nq.X * nq.Z)));

            var yawZ = MathF.Atan2(
                2 * (nq.W * nq.Z + nq.X * nq.Y),
                1 - 2 * (nq.Y * nq.Y + nq.Z * nq.Z));

            return new Vector3(rollX, pitchY, yawZ);
        }

        public static Quaternion ToQuaternion(this Vector4 rotation)
        {
            if (new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W) is Quaternion q
                && q.IsApproximately(Quaternion.Identity))
            {
                return Quaternion.Identity;
            }
            return q;
        }

        public static Quaternion ToQuaternion(this hkQuaternionf rotation)
        {
            if (new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W) is Quaternion q
                && q.IsApproximately(Quaternion.Identity))
            {
                return Quaternion.Identity;
            }
            return q;
        }

        public static Quaternion ToQuaternion(this hkVector4f rotation)
        {
            if (new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W) is Quaternion q
                && q.IsApproximately(Quaternion.Identity))
            {
                return Quaternion.Identity;
            }
            return q;
        }


        public static hkQuaternionf ToHavokRotation(this Quaternion rot)
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
                Z = rot.Z,
                W = rot.W
            };
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

        public static hkVector4f ToHavokVector(this FFXIVClientStructs.FFXIV.Common.Math.Vector3 vec)
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

        public static hkVector4f ToHavokVector(this FFXIVClientStructs.FFXIV.Common.Math.Vector4 vec)
        {
            return new hkVector4f
            {
                X = vec.X,
                Y = vec.Y,
                Z = vec.Z,
                W = 1.0f
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
                return Vector3.One;
            }
            return v;
        }

        public static Vector3 GetAsNumericsVector(this hkVector4f vec)
        {
            Vector3 v = new Vector3(vec.X, vec.Y, vec.Z);

            if (v.IsApproximately(Vector3.Zero))
            {
                return Vector3.Zero;
            }
            else if (v.IsApproximately(Vector3.One))
            {
                return Vector3.One;
            }
            return v;
        }

        public static Vector4 GetAsNumericsVector(this Quaternion q)
        {
            Vector4 v = new Vector4(q.X, q.Y, q.Z, q.W);

            if (v.IsApproximately(Vector4.Zero))
            {
                return Vector4.Zero;
            }
            else if (v.IsApproximately(Vector4.One))
            {
                return Vector4.One;
            }
            return v;
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