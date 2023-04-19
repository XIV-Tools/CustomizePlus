// © Customize+.
// Licensed under the MIT license.

using CustomizePlus.Memory;
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
	}
}
