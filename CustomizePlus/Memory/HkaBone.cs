// © Customize+.
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace CustomizePlus.Memory
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public unsafe struct HkaBone
    {
        [FieldOffset(0x000)] public byte* Name;

        public string? GetName()
        {
            return Marshal.PtrToStringAnsi(new IntPtr(Name));
        }
    }
}