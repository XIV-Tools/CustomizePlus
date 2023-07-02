// © Customize+.
// Licensed under the MIT license.

using System;
using FFXIVClientStructs.FFXIV.Common.Math;
using CustomizePlus.Data;
using FFXIVClientStructs.Havok;

//using FFXIVClientStructs.FFXIV.Client.Graphics;

namespace CustomizePlus.Extensions
{
    internal static class TransformExtensions
    {
        public static bool Equals(this hkQsTransformf first, hkQsTransformf second)
        {
            return first.Translation.Equals(second.Translation)
                   && first.Rotation.Equals(second.Rotation)
                   && first.Scale.Equals(second.Scale);
        }

        public static bool IsNull(this hkQsTransformf t)
        {
            return t.Equals(Constants.NullTransform);
        }

        public static Vector4 GetAttribute(this hkQsTransformf t, BoneAttribute att)
        {
            return att switch
            {
                BoneAttribute.Position => t.Translation.ToClientVector4(),
                BoneAttribute.Rotation => t.Rotation.ToClientQuaternion().ToClientVector4(),
                BoneAttribute.Scale => t.Scale.ToClientVector4(),
                _ => throw new NotImplementedException()
            };
        }
    }
}