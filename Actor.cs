// © Anamnesis.
// Developed by W and A Walsh.
// Licensed under the MIT license.

namespace Anamnesis.Memory
{
	using System;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Explicit)]
	public struct Actor
	{
		public const int ObjectKindOffset = 0x008c;
		public const int RenderModeOffset = 0x0104;

		[FieldOffset(0x0030)]
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
		public string Name;

		[FieldOffset(ObjectKindOffset)] public ActorTypes ObjectKind;
		[FieldOffset(0x00F0)] public IntPtr ModelObject;
		[FieldOffset(0x1898)] public Appearance Customize;
	}
}
