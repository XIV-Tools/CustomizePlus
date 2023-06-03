// © Customize+.
// Licensed under the MIT license.

using System.Runtime.InteropServices;

namespace CustomizePlus.Memory
{
    [StructLayout(LayoutKind.Sequential, Size = 0x030)]
    public struct Transform
    {
        public HkVector4 Translation;
        public HkVector4 Rotation;
        public HkVector4 Scale;
    }
}