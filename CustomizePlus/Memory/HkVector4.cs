// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Memory
{
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential)]
	public struct HkVector4
	{
		public float X;
		public float Y;
		public float Z;
		public float W;

		public override string ToString()
		{
			return $"({this.X}, {this.Y}, {this.Z}, {this.W})";
		}
	}
}
