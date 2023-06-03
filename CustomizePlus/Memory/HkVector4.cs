// © Customize+.
// Licensed under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CustomizePlus.Memory
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HkVector4
    {
        public static readonly HkVector4 Zero = new(0, 0, 0, 0);
        public static readonly HkVector4 One = new(1, 1, 1, 1);

        public float X;
        public float Y;
        public float Z;
        public float W;

        public HkVector4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4 GetAsNumericsVector(bool round = true)
        {
            if (round)
            {
                return new Vector4(MathF.Round(X, 3), MathF.Round(Y, 3), MathF.Round(Z, 3), MathF.Round(W, 3));
            }

            return new Vector4(X, Y, Z, W);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z}, {W})";
        }
    }
}