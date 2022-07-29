// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Memory
{
	using System;
	using System.Runtime.InteropServices;
	using System.Numerics;

	[StructLayout(LayoutKind.Sequential)]
	public struct HkVector4
	{
		public static readonly HkVector4 One = new HkVector4(1, 1, 1, 1);

		public float X;
		public float Y;
		public float Z;
		public float W;

		public HkVector4(float x, float y, float z, float w)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
			this.W = w;
		}

		public Vector4 GetAsNumericsVector()
		{
			return new Vector4(MathF.Round(this.X,3), MathF.Round(this.Y,3), MathF.Round(this.Z,3), MathF.Round(this.W,3));
		}

		public override string ToString()
		{
			return $"({this.X}, {this.Y}, {this.Z}, {this.W})";
		}


		public bool IsApproximately(HkVector4 other, bool includeW, float errorMargin = 0.001f)
		{
			if (includeW)
			{
				return IsApproximately(this.X, other.X, errorMargin)
					&& IsApproximately(this.Y, other.Y, errorMargin)
					&& IsApproximately(this.Z, other.Z, errorMargin)
					&& IsApproximately(this.W, other.W, errorMargin);
			}
			else
			{
				return IsApproximately(this.X, other.X, errorMargin)
					&& IsApproximately(this.Y, other.Y, errorMargin)
					&& IsApproximately(this.Z, other.Z, errorMargin);
			}
		}

		private static bool IsApproximately(float a, float b, float errorMargin)
		{
			float d = MathF.Abs(a - b);
			return d < errorMargin;
		}


	}
}
