// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Memory;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
			float d = MathF.Abs(a - b);
			return d < errorMargin;
		}

		public static Quaternion ToQuaternion(this Vector3 rotation)
		{
			return Quaternion.CreateFromYawPitchRoll(
				rotation.X * MathF.PI / 180,
				rotation.Y * MathF.PI / 180,
				rotation.Z * MathF.PI / 180);
		}

		public static Vector3 ToEulerAngles(this Quaternion r)
		{
			float  yaw = MathF.Atan2(2.0f * ((r.Y * r.W) + (r.X * r.Z)), 1.0f - (2.0f * ((r.X * r.X) + (r.Y * r.Y))));
			float pitch = MathF.Asin(2.0f * ((r.X * r.W) - (r.Y * r.Z)));
			float roll = MathF.Atan2(2.0f * ((r.X * r.Y) + (r.Z * r.W)), 1.0f - (2.0f * ((r.X * r.X) + (r.Z * r.Z))));

			return new Vector3(yaw, pitch, roll);
		}

		public static Quaternion ToQuaternion(this Vector4 rotation)
		{
			return new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
		}

		public static Quaternion ToQuaternion(this Memory.HkVector4 rotation)
		{
			return new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W);
		}

		public static Memory.HkVector4 ToHavokVector(this Quaternion rotation)
		{
			return new HkVector4(rotation.X, rotation.Y, rotation.Z, rotation.W);
		}

		public static Memory.HkVector4 ToHavokVector(this Vector4 vec)
		{
			return new HkVector4(vec.X, vec.Y, vec.Z, vec.W);
		}

		public static Vector3 CullW(this Vector4 vec)
		{
			return new Vector3(vec.X, vec.Y, vec.Z);
		}

		public static Vector4 AppendW(this Vector3 vec, float w = 0.0f)
		{
			return new Vector4(vec.X, vec.Y, vec.Z, w);
		}
	}
}
