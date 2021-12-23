// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Memory
{
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential, Size=0x030)]
	public struct Transform
	{
		public HkVector4 Translation;
		public HkVector4 Rotation;
		public HkVector4 Scale;
	}
}
