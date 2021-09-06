// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.GameStructs
{
	using System;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Explicit)]
	public struct Actor
	{
		[FieldOffset(0x0030)]
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
		public string Name;

		[FieldOffset(0x008c)] public ActorTypes ObjectKind;
		[FieldOffset(0x00F0)] public IntPtr ModelObject;
		[FieldOffset(0x1898)] public Appearance Customize;
	}
}
