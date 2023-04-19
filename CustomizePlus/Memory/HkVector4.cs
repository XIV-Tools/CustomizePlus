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
		public static readonly HkVector4 Zero = new HkVector4(0, 0, 0, 0);
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

		public Vector4 GetAsNumericsVector(bool round = true)
		{
			if(round)
				return new Vector4(MathF.Round(this.X, 3), MathF.Round(this.Y, 3), MathF.Round(this.Z, 3), MathF.Round(this.W, 3));

			return new Vector4(this.X, this.Y, this.Z, this.W);
		}

		public override string ToString()
		{
			return $"({this.X}, {this.Y}, {this.Z}, {this.W})";
		}
	}
}
