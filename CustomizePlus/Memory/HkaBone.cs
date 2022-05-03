// © Customize+.
// Licensed under the MIT license.

namespace CustomizePlus.Memory
{
	using System;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Explicit, Size=16)]
	public unsafe struct HkaBone
	{
		[FieldOffset(0x000)] public byte* Name;

		public string? GetName()
		{
			return Marshal.PtrToStringAnsi(new IntPtr(this.Name));
		}
	}
}
